using BroadcastPluginSDK.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.FlightSimulator.SimConnect;
using Microsoft.Win32;
using MSFSPlugin.Forms;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Timer = System.Threading.Timer;

namespace MSFSPlugin.Classes
{
    internal class SystemStateRequests
    {
        public int RequestId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DataTypeName { get; set; } = string.Empty;
        public bool Subscribed { get; set; } = false;

        public SystemStateRequests(int id, string name, string type)
        {
            RequestId = id;
            Name = name;
            DataTypeName = type;
        }
    }

    /// <summary>
    /// Represents a connection to the Microsoft Flight Simulator using SimConnect.
    /// Provides methods to manage simulator data requests and handle connection events.
    /// </summary>
    public partial class FlightSimulator : IDisposable
    {
        private const uint WM_USER_SIMCONNECT = 0x0402;

        #region Private variables
        private MeasurementUnits measurementUnits = new MeasurementUnits();
        private SimConnect? m_oSimConnect = null;
        private DisplayLogging? logger = null;
        private Timer? simPollTimer;
        private TimeSpan pollInterval = TimeSpan.FromMilliseconds(1000);

        private readonly SimVarRequestRegistry requestManager = new();
        private readonly SimConnectDataRegistry _registry = new();
        private uint nextId = 1;

        #endregion

        public SimConnect? Connection { get => m_oSimConnect; }
        public event EventHandler<CacheData>? DataReceived;
        public event EventHandler<bool>? ConnectionStatusChanged;
        public bool isConnected { get; private set; } = false;
        private bool ConnectToSim()
        {
            if (m_oSimConnect is null || !isConnected )
            {
                try
                {
                    m_oSimConnect = new SimConnect("Broadcast", (IntPtr)null, WM_USER_SIMCONNECT, null, 0);
                    RequestSystemInformation();

                    if (m_oSimConnect is not null)
                    {
                        logger?.LogInformation("SimConnect connection established.");

                        m_oSimConnect.OnRecvOpen += SimConnect_OnRecvOpen;
                        m_oSimConnect.OnRecvSimobjectDataBytype += HandleSimData;
                        m_oSimConnect.OnRecvEvent += SimConnect_OnRecvEvent;
                        m_oSimConnect.OnRecvQuit += SimConnect_OnRecvQuit;
                        m_oSimConnect.OnRecvException += SimConnect_OnRecvException;
                        m_oSimConnect.OnRecvSystemState += SimConnect_OnRecvSystemState;

                        UpdateConnectionStatus(true);
                    }
                }
                catch (COMException)
                {
                    logger?.LogError("SimConnect connection failed. Is MSFS running?");
                    m_oSimConnect = null;
                    UpdateConnectionStatus(false);
                }
                catch (Exception ex)
                {
                    logger?.LogError($"Unexpected error during SimConnect connection: {ex.Message}");
                    m_oSimConnect = null;
                    UpdateConnectionStatus(false);
                }
            }
            else
            {
                try
                {
                    m_oSimConnect?.ReceiveMessage();
                }
                catch (COMException)
                {
                    logger?.LogWarning("SimConnect connection lost.");
                    m_oSimConnect = null;
                    UpdateConnectionStatus(false);
                    return false;
                }
                catch (Exception ex)
                {
                    logger?.LogError($"Unexpected error during SimConnect message receive: {ex.Message}");
                    m_oSimConnect = null;
                    UpdateConnectionStatus(false);
                    return false;
                }
            }
            UpdateConnectionStatus(m_oSimConnect is not null);
            return m_oSimConnect is not null;
        }

        private void PollSimConnect(object? state)
        {
            try
            {
                ConnectToSim();
                MakeRequests();
            }
            catch (Exception ex)
            {
                logger?.LogError($"Polling error: {ex.Message}");
            }
        }
        
        private static bool Requested = false;
        private void RequestSystemInformation()
        {
            if ( Requested ) return;
            Requested = true;

            foreach (Events req in Enum.GetValues(typeof(Events)))
            {
                m_oSimConnect?.SubscribeToSystemEvent( req, req.ToEventName() );
                logger?.LogInformation($"Subscribed to system event: {req.ToEventName()} with ID: {req}");
                m_oSimConnect?.SetSystemEventState(    req, SIMCONNECT_STATE.ON);
            }

            m_oSimConnect?.ReceiveMessage();
        }

        private void SimConnect_OnRecvSystemState(SimConnect sender, SIMCONNECT_RECV_SYSTEM_STATE recEvent)
        {
            logger?.LogInformation($"SimConnect_OnRecvSystemState {recEvent}");
        }

        private void SimConnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            logger?.LogInformation("Received Quit Message");
            UpdateConnectionStatus(false);
        }

        private void SimConnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            SIMCONNECT_EXCEPTION ex = (SIMCONNECT_EXCEPTION)data.dwException;

            logger?.LogWarning($"Received Exception Message {ex} - Send Id: {data.dwSendID} , Index: {data.dwIndex} , Id: {data.dwID}");

            // UpdateConnectionStatus(false);
        }

        private void UpdateConnectionStatus(bool connected)
        {
            if (isConnected != connected)
            {
                isConnected = connected;
                ConnectionStatusChanged?.Invoke(this, connected);
                logger?.LogInformation($"SimConnect status changed: {(connected ? "Connected" : "Disconnected")}");
            }

            if (!connected)
            {
                requestManager.GetAllRequests().ToList().ForEach(r => r.Value = null);
                Requested = false;
                var send = new CacheData()
                {
                    Data = new Dictionary<string, string> { { "DISCONNECTED", DateTime.Now.ToString() } },
                    Prefix = CachePrefixes.SYSTEM
                };
                DataReceived?.Invoke(this, send);
                pollInterval = TimeSpan.FromMilliseconds(1000);
            }
            else
            {
                // Connected lets get data faster
                pollInterval = TimeSpan.FromMilliseconds(100);
            }
            
            simPollTimer?.Change(pollInterval, pollInterval);
        }
        private void SimConnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            logger?.LogDebug("SimConnect_OnRecvOpen");
        }
        private void SimConnect_OnRecvEvent(SimConnect sender, SIMCONNECT_RECV_EVENT recEvent)
        {
            Events events = (Events)recEvent.uEventID;
            logger?.LogInformation($"SimConnect_OnRecvEvent: EventID={events.ToString()} , Data={recEvent.dwData}");

            Dictionary<string, string> eventData = new();

            if (events == Events.EVENT_PAUSE)
            {
                eventData["PAUSED"] = (recEvent.dwData == 1) ? "True" : "False";
            }

            var send = new CacheData()
            {
                Data = eventData,
                Prefix = CachePrefixes.SYSTEM
            };

            DataReceived?.Invoke(this, send);

        }

        private void HandleSimData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data)
        {
            logger?.LogDebug($"HandleSimData: RequestID={data.dwRequestID}, ObjectID={data.dwObjectID}");

            try
            {
                if (!requestManager.TryGetByRequestId((REQUEST)data.dwRequestID, out var request) || request is null)
                    return;

                string value = string.Empty;

                logger?.LogDebug($"Processing data for request: {request.Name}, Type: {request.DataTypeName}");

                if (request.DataTypeName.Equals("STRING256", StringComparison.OrdinalIgnoreCase))
                {
                    SimVarStringStruct s = (SimVarStringStruct)data.dwData[0];
                    value = s.Value;
                }
                else if (request.DataTypeName.Equals("FLOAT64", StringComparison.OrdinalIgnoreCase))
                {
                    double d = (double)data.dwData[0];
                    value = d.ToString();
                }

                logger?.LogDebug($"Received {value.GetType().Name} value for {request.Name}: {value}");
                requestManager.UpdateValue(request.Name, value);

                var send = new CacheData()
                {
                    Data = new Dictionary<string, string> { { request.Name, value } },
                    Prefix = CachePrefixes.DATA
                };

                DataReceived?.Invoke(this, send);
            }
            catch (Exception ex)
            {
                logger?.LogError($"{ex} Error processing SimConnect data for request ID {data.dwRequestID}.");
            }
        }

        public void Dispose()
        {
            simPollTimer?.Dispose();
            m_oSimConnect?.Dispose();
        }

        public FlightSimulator(DisplayLogging logger)
        {
            this.logger = logger;
            simPollTimer = new Timer(PollSimConnect, null, pollInterval, pollInterval);
        }
        private void MakeRequests()
        {
            if (m_oSimConnect is null)
            {
                logger?.LogDebug("SimConnect is not connected. Cannot make requests.");
                return;
            }

            // Requested data
            foreach (SimVarRequest oSimvarRequest in requestManager.GetAllRequests())
            {
                logger?.LogDebug($"Registered Request: {oSimvarRequest.Name}, " +
                                 $"Definition ID: {oSimvarRequest.DefinitionId}, " +
                                 $"Request ID: {oSimvarRequest.RequestId}, " +
                                 $"Unit: {oSimvarRequest.Unit}, " +
                                 $"Name: {oSimvarRequest.Name}, " +
                                 $"Type: {oSimvarRequest.DataTypeName}");

                try
                {
                    m_oSimConnect?.RequestDataOnSimObjectType(oSimvarRequest.RequestId, oSimvarRequest.DefinitionId, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);

                    logger?.LogDebug($"Data request sent for {oSimvarRequest.Name} with Request ID: {oSimvarRequest.RequestId} and Definition ID: {oSimvarRequest.DefinitionId}");
                }
                catch (Exception ex)
                {
                    logger?.LogError($"{ex.Message} Error requesting data for {oSimvarRequest.Name}  Data Type: {oSimvarRequest.DataTypeName}");
                }
            }
        }
        public void AddRequest(string name)
        {
            if (m_oSimConnect is null || isConnected is false) return;

            Datum Measure = measurementUnits.FindUnitByName(name);
            if(Measure.Name == string.Empty)
            {
                logger?.LogError($"Unknown variable name: {name}");
                return;
            }

            if (!Enum.TryParse<SIMCONNECT_DATATYPE>(Measure.Type, true, out var datatype))
            {
                logger?.LogError($"Unknown SIMCONNECT_DATATYPE: {Measure.Type} for {name} aka {Measure.Name}");
                return;
            }

            logger?.LogInformation($"Adding Request {name}:{Measure.Name}: {Measure.Measure} , {datatype} ");

            var requestId = nextId++;
            var definitionId = nextId++;

            SimVarRequest oSimvarRequest = new SimVarRequest
            {
                DefinitionId = (DEFINITION)definitionId,
                RequestId = (REQUEST)requestId,
                Name = Measure.Name,
                DataTypeName = Measure.Type,
                Unit = Measure.Measure
            };

            requestManager.RegisterRequest(oSimvarRequest);
            CreateDataDefinitions();

            logger?.LogDebug($"Added Data Definition: {oSimvarRequest.Name}, " +
                             $"Definition ID: {oSimvarRequest.DefinitionId}, " +
                             $"Request ID: {oSimvarRequest.RequestId}, ");
        }
     
        private void CreateDataDefinitions()
        {
            foreach( var request in requestManager.GetAllRequests() )
            {
                if (!Enum.TryParse<SIMCONNECT_DATATYPE>(request.DataTypeName, true, out var datatype))
                {
                    logger?.LogError($"Unknown SIMCONNECT_DATATYPE: {request.DataTypeName}");
                    datatype = SIMCONNECT_DATATYPE.FLOAT64;
                }

                var defId = _registry.GetByName(request.Name);
                if (defId == null)
                {
                    m_oSimConnect?.AddToDataDefinition(request.DefinitionId, request.Name, request.Unit, datatype, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                    _registry.Add(request.DefinitionId, request.Name); // Track it

                    if (datatype == SIMCONNECT_DATATYPE.STRING256)
                        m_oSimConnect?.RegisterDataDefineStruct<SimVarStringStruct>(request.DefinitionId);
                    else
                        m_oSimConnect?.RegisterDataDefineStruct<double>(request.DefinitionId);
                }
            }
        }

    }
}

