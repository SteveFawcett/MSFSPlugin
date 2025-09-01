using Microsoft.FlightSimulator.SimConnect;
using MSFSPlugin.Forms;
using System.Runtime.InteropServices;

namespace MSFSPlugin.Classes
{
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
        private IntPtr hWnd = IntPtr.Zero;
        private DisplayLogging? logger = null;
        private System.Windows.Forms.Timer requestTimer = new();
        private readonly SimVarRequestRegistry requestManager = new();
        private uint nextId = 1000;
        public event EventHandler<Dictionary<string, string>>? DataReceived;
        #endregion

        public SimConnect? Connection { get => m_oSimConnect; }
        public event EventHandler<bool>? ConnectionStatusChanged;
        public bool isConnected { get; private set; } = false;
        public bool ConnectToSim()
        {
            if (hWnd == IntPtr.Zero)
            {
                logger?.LogError("Invalid window handle. Cannot connect to SimConnect.");
                UpdateConnectionStatus(false);
                return false;
            }

            if (m_oSimConnect is null)
            {
                InitializeSimConnect();
            }

            UpdateConnectionStatus(m_oSimConnect is not null);
            return m_oSimConnect is not null;
        }

        /// <summary>
        /// Initializes the SimConnect connection and sets up event handlers.
        /// </summary>
        private void InitializeSimConnect()
        {
            try
            {
                m_oSimConnect = new SimConnect("SimListener", hWnd, WM_USER_SIMCONNECT, null, 0);

                if (m_oSimConnect is not null)
                {
                    logger?.LogInformation("SimConnect connection established.");

                    m_oSimConnect.OnRecvOpen += SimConnect_OnRecvOpen;
                    m_oSimConnect.OnRecvSimobjectDataBytype += HandleSimData;
                    m_oSimConnect.OnRecvEvent += SimConnect_OnRecvEvent;
                    m_oSimConnect.OnRecvQuit += SimConnect_OnRecvQuit;
                    m_oSimConnect.OnRecvException += SimConnect_OnRecvException;
                    try
                    {
                        m_oSimConnect.SubscribeToSystemEvent(Event.RECUR_1SEC, "1sec");
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError($"{ex.Message}: Failed to subscribe to system event.");
                    }
                }
            }
            catch (COMException)
            {
                logger?.LogError("SimConnect connection failed. Is MSFS running?");
            }
            catch (Exception)
            {
                logger?.LogError("Unexpected error during SimConnect connection.");
            }
        }

        private void SimConnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            logger?.LogInformation("Received Quit Message");
            UpdateConnectionStatus(false);
        }

        private void SimConnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            // TODO: Need to capture exceptions and handle them
            // logger?.LogInformation("Received Exception Message");    
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
        }
        private void SimConnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            logger?.LogDebug("SimConnect_OnRecvOpen");
        }
        private void SimConnect_OnRecvEvent(SimConnect sender, SIMCONNECT_RECV_EVENT recEvent)
        {
            if (recEvent.uEventID == (uint)Event.RECUR_1SEC)
            {
                MakeRequests();
            }
        }

        private void HandleSimData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data)
        {
            logger?.LogDebug($"HandleSimData: RequestID={data.dwRequestID}, ObjectID={data.dwObjectID}");

            try
            {
                if (!requestManager.TryGetByRequestId((REQUEST)data.dwRequestID, out var request) || request is null)
                    return;

                string value = string.Empty;

                if (request.DataTypeName.Equals("STRING256", StringComparison.OrdinalIgnoreCase))
                {
                    SimVarStringStruct s = (SimVarStringStruct)data.dwData[0];
                    value = s.Value;
                }
                else if (request.DataTypeName.Equals("FLOAT32", StringComparison.OrdinalIgnoreCase))
                {
                    double d = (double)data.dwData[0];
                    value = d.ToString();
                }

                logger?.LogDebug($"Received {value.GetType().Name} value for {request.Name}: {value}");
                requestManager.UpdateValue(request.Name, value);
                DataReceived?.Invoke(this, new Dictionary<string, string> { { request.Name, value } });
            }
            catch (Exception ex)
            {
                logger?.LogError($"{ex} Error processing SimConnect data for request ID {data.dwRequestID}.");
            }
        }

        public void Dispose()
        {
            if (m_oSimConnect is not null) m_oSimConnect.Dispose();
        }

        public FlightSimulator(DisplayLogging logger, IntPtr hWnd)
        {
            this.hWnd = hWnd;
            this.logger = logger;
        }
        private void MakeRequests()
        {
            if (m_oSimConnect is null)
            {
                logger?.LogDebug("SimConnect is not connected. Cannot make requests.");
                return;
            }

            foreach (SimVarRequest oSimvarRequest in requestManager.GetAllRequests())
            {
                logger?.LogDebug($"Registered Request: {oSimvarRequest.Name}, " +
                                 $"Definition ID: {oSimvarRequest.DefinitionId}, " +
                                 $"Request ID: {oSimvarRequest.RequestId}, " +
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
        public void AddRequest(string name )
        {
            if (m_oSimConnect is null || isConnected is false) return;

            Datum Measure = measurementUnits.FindUnitByName(name);

            if (!Enum.TryParse<SIMCONNECT_DATATYPE>(Measure.Type, true, out var datatype))
            {
                logger?.LogError($"Unknown SIMCONNECT_DATATYPE: {Measure.Type} for {name}");
                return;
            }

            logger?.LogInformation($"Adding Request {Measure.Name} : {Measure.Measure} , {Measure.Type} ");

            var requestId = nextId++;
            var definitionId = nextId++;

            SimVarRequest oSimvarRequest = new SimVarRequest
            {
                DefinitionId = (DEFINITION)definitionId,
                RequestId = (REQUEST)requestId,
                Name = name,
                DataTypeName = Measure.Type,
                Unit = Measure.Measure
            };

            requestManager.RegisterRequest(oSimvarRequest);

            logger?.LogDebug($"Adding Data Definition: {oSimvarRequest.Name}, " +
                             $"Definition ID: {oSimvarRequest.DefinitionId}, " +
                             $"Request ID: {oSimvarRequest.RequestId}, ");

   
            m_oSimConnect.AddToDataDefinition(oSimvarRequest.DefinitionId, oSimvarRequest.Name, "", datatype, 0.0f, SimConnect.SIMCONNECT_UNUSED);
         
            if ( datatype == SIMCONNECT_DATATYPE.STRING256 )
                m_oSimConnect.RegisterDataDefineStruct<SimVarStringStruct>(oSimvarRequest.DefinitionId);
            else
                m_oSimConnect.RegisterDataDefineStruct<double>( oSimvarRequest.DefinitionId);

            MakeRequests();

        }

    }
}

