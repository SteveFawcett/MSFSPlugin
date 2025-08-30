using BroadcastPluginSDK.Classes;
using Microsoft.FlightSimulator.SimConnect;
using MSFSPlugin.Forms;
using System.Collections.ObjectModel;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

namespace MSFSPlugin.Classes
{
    /// <summary>
    /// Represents a connection to the Microsoft Flight Simulator using SimConnect.
    /// Provides methods to manage simulator data requests and handle connection events.
    /// </summary>
    public partial class FlightSimulator : IDisposable
    {
        #region Private variables
        private SimConnect? m_oSimConnect = null;
        private ObservableCollection<SimvarRequest> lSimvarRequests = [];
        private string AircaftLoaded = UnknownAircraft;
        private uint m_iCurrentDefinition = 0;
        private uint m_iCurrentRequest = 0;
        private IntPtr hWnd = IntPtr.Zero;
        private readonly object simvarRequestsLock = new();
        private DisplayLogging? logger = null;

        public event EventHandler<Dictionary<string, string>>? DataReceived;
        #endregion

        public SimConnect? Connection { get => m_oSimConnect; }

        public event EventHandler<bool>? ConnectionStatusChanged;
        public bool isConnected { get; private set; } = false;

        public bool ConnectToSim()
        {
            if (m_oSimConnect is null)
            {
                try
                {
                    m_oSimConnect = new SimConnect("SimListener", hWnd, WM_USER_SIMCONNECT, null, 0);

                    if (m_oSimConnect is not null)
                    {
                        logger?.LogInformation("SimConnect connection established.");
                        m_oSimConnect.OnRecvOpen += SimConnect_OnRecvOpen;
                        m_oSimConnect.OnRecvSimobjectDataBytype += SimConnect_OnRecvSimobjectDataBytype;
                        m_oSimConnect.OnRecvSystemState += SimConnect_OnRecvEvent;
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

            UpdateConnectionStatus(m_oSimConnect is not null);
            return m_oSimConnect is not null;
        }

        private void ReceiveSimConnectMessage()
        {
            try
            {
                m_oSimConnect?.ReceiveMessage();
                UpdateConnectionStatus(true);
            }
            catch (Exception ex)
            {
                logger?.LogError( $"{ex} Error receiving SimConnect message.");
                UpdateConnectionStatus(false);
            }
        }
        private bool RegisterToSimConnect(SimvarRequest _oSimvarRequest)
        {
            if (m_oSimConnect != null)
            {
                try
                {
                    if (_oSimvarRequest.bIsString)
                    {
                        logger?.LogInformation($"Registering string SimvarRequest: {_oSimvarRequest.sName} with Definition ID: {_oSimvarRequest.eDef} and Request ID: {_oSimvarRequest.eRequest}");
                        m_oSimConnect.AddToDataDefinition(_oSimvarRequest.eDef, _oSimvarRequest.sName, "", SIMCONNECT_DATATYPE.STRING256, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                        m_oSimConnect.RegisterDataDefineStruct<SimvarRequest>(_oSimvarRequest.eDef);
                    }
                    else
                    {
                        logger?.LogInformation($"Registering double SimvarRequest: {_oSimvarRequest.sName} with Definition ID: {_oSimvarRequest.eDef} and Request ID: {_oSimvarRequest.eRequest}");
                        m_oSimConnect.AddToDataDefinition(_oSimvarRequest.eDef, _oSimvarRequest.sName, _oSimvarRequest.sUnits, SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                        m_oSimConnect.RegisterDataDefineStruct<double>(_oSimvarRequest.eDef);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    logger?.LogError($"{ex} Error processing SimobjectData for {_oSimvarRequest.sName}");
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        private void SimConnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            UpdateConnectionStatus(false);
        }

        private void SimConnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            UpdateConnectionStatus(false);
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

            foreach (SimvarRequest oSimvarRequest in lSimvarRequests)
            {
                try
                {
                    RegisterToSimConnect(oSimvarRequest);
                }
                catch (Exception ex)
                {
                    logger?.LogError($"{ex} Error processing SimobjectData for {oSimvarRequest.sName}");
                }
            }
        }
        private void SimConnect_OnRecvEvent(SimConnect sender, SIMCONNECT_RECV_SYSTEM_STATE data)
        {
            try
            {
                if ((Requests)data.dwRequestID == Requests.AIRCRAFT_LOADED)
                {
                    AircaftLoaded = data.szString;
                }
            }
            catch (Exception ex)
            {
                logger?.LogError( $"{ex} Error processing SimConnect_OnRecvEvent.");
            }
        }


        // Replace the method with thread-safe access
        private void SimConnect_OnRecvSimobjectDataBytype(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data)
        {
            logger?.LogDebug( $"Received SimObject Data for Request ID: {data.dwRequestID}");
            Dictionary<string, string> AircraftData = [];

            uint iRequest = data.dwRequestID;
            lock (simvarRequestsLock)
            {
                if (lSimvarRequests != null)
                {
                    foreach (SimvarRequest oSimvarRequest in lSimvarRequests)
                    {
                        if (iRequest == (uint)oSimvarRequest.eRequest)
                        {
                            if (string.IsNullOrEmpty(oSimvarRequest.sName))
                            {
                                logger?.LogError($"Request {iRequest} has no name. Skipping.");
                                continue;
                            }

                            try
                            {
                                if (oSimvarRequest.bIsString)
                                {
                                    ResultStructure result = (ResultStructure)data.dwData[0];
                                    oSimvarRequest.dValue = 0;
                                    oSimvarRequest.sValue = result.sValue;
                                    AircraftData.Add( oSimvarRequest.sName, oSimvarRequest.sValue ?? string.Empty );
                                    logger?.LogDebug($"Received string value: {oSimvarRequest.sValue} for request: {oSimvarRequest.sName}");
                                }
                                else
                                {
                                    double dValue = (double)data.dwData[0];
                                    oSimvarRequest.dValue = dValue;
                                    oSimvarRequest.sValue = dValue.ToString("F9");
                                    AircraftData.Add(oSimvarRequest.sName, oSimvarRequest.dValue.ToString());
                                    logger?.LogDebug($"Received double value: {dValue} for request: {oSimvarRequest.sName}");
                                }
                            }
                            catch (Exception ex)
                            {
                                logger?.LogError($"{ex} Error processing SimobjectData for {oSimvarRequest.sName}");
                            }
                        }
                    }
                }
            }
            DataReceived?.Invoke(this, AircraftData);
            MakeRequests();
        }


        public void Dispose()
        {
           if( m_oSimConnect is not null ) m_oSimConnect.Dispose();
        }

        public FlightSimulator( DisplayLogging logger , IntPtr hWnd)
        {
            this.hWnd = hWnd;
            this.logger = logger;
            lSimvarRequests = [];
        }

        private void MakeRequests()
        {
            logger?.LogDebug("Making data requests to SimConnect.");

            foreach (SimvarRequest oSimvarRequest in lSimvarRequests)
            {
                try
                {
                    logger?.LogDebug($"Requesting data for {oSimvarRequest.sName}");
                    m_oSimConnect?.RequestDataOnSimObjectType(oSimvarRequest.eRequest, oSimvarRequest.eDef, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);

                }
                catch (Exception ex)
                {
                    logger?.LogError($"Error requesting data for {oSimvarRequest.sName}");
                }
            }   
        }
        public void AddRequest(string _sNewSimvarRequest, string _sNewUnitRequest, bool _bIsString)
        {
            logger?.LogInformation($"Adding Request {_sNewSimvarRequest} ");

            if (m_oSimConnect is null)
            {
                logger?.LogDebug("SimConnect is not connected. Cannot add request.");
                throw new SimulatorNotConnectedException("SimConnect is not connected. Cannot add request.");
            }

            if (m_iCurrentDefinition >= (uint)DEFINITION.MAX_DEFINITIONS || m_iCurrentRequest >= (uint)REQUEST.MAX_REQUESTS)
            {
                logger?.LogError("Maximum definitions or requests reached. Cannot add more.");
                return;
            }

            SimvarRequest oSimvarRequest = new SimvarRequest
            {
                eDef = (DEFINITION)m_iCurrentDefinition,
                eRequest = (REQUEST)m_iCurrentRequest,
                sName = _sNewSimvarRequest,
                bIsString = _bIsString,
                sUnits = _bIsString ? null : _sNewUnitRequest
            };

            try
            {
                logger?.LogDebug($"Registering SimvarRequest: {_sNewSimvarRequest} with Definition ID: {oSimvarRequest.eDef} and Request ID: {oSimvarRequest.eRequest}");
                RegisterToSimConnect(oSimvarRequest);
            }
            catch (Exception ex)
            {
                logger?.LogError( $"{ex.Message} Failed to register SimvarRequest: {_sNewSimvarRequest}");
            }

            lSimvarRequests.Add(oSimvarRequest);

            ++m_iCurrentDefinition;
            ++m_iCurrentRequest;
            logger?.LogDebug($"Request {_sNewSimvarRequest} added with Definition ID: {oSimvarRequest.eDef} and Request ID: {oSimvarRequest.eRequest}");

            MakeRequests();
        }
    }
}

