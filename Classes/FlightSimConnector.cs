using Microsoft.FlightSimulator.SimConnect;
using MSFSPlugin.Forms;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace MSFSPlugin.Classes
{
    /// <summary>
    /// Represents a connection to the Microsoft Flight Simulator using SimConnect.
    /// Provides methods to manage simulator data requests and handle connection events.
    /// </summary>
    public partial class Connect 
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
        #endregion

        #region Private Methods

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
                        m_oSimConnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(SimConnect_OnRecvOpen);
                        m_oSimConnect.OnRecvSimobjectDataBytype += new SimConnect.RecvSimobjectDataBytypeEventHandler(SimConnect_OnRecvSimobjectDataBytype);
                        m_oSimConnect.OnRecvSystemState += new SimConnect.RecvSystemStateEventHandler(SimConnect_OnRecvEvent);

                        try
                        {
                            m_oSimConnect.SubscribeToSystemEvent(Event.RECUR_1SEC, "1sec");
                        }
                        catch (Exception ex)
                        {
                            logger?.LogError($"{ex.Message}: Failed to subscribe to system event.");
                            return false;
                        }

                    }
                }
                catch (COMException)
                {
                    logger?.LogError("SimConnect connection failed. Is MSFS running?");
                    m_oSimConnect = null;
                    return false;
                }
                catch (Exception)
                {
                    logger?.LogError("Unexpected error during SimConnect connection.");
                    m_oSimConnect = null;
                    return false;
                }
            }

            return m_oSimConnect is not null;
        }
        private void InternalAddRequest(string _sNewSimvarRequest, string _sNewUnitRequest, bool _bIsString)
        {
            logger?.LogInformation($"AddRequest {_sNewSimvarRequest}");


            if (!ValidateRequest(_sNewSimvarRequest))
            {
                logger?.LogError($"Invalid request: {_sNewSimvarRequest}. Skipping.");
                throw new InvalidSimDataRequestException($"Invalid request: {_sNewSimvarRequest}. Skipping.");
            }

            if (m_oSimConnect is null)
            {
                logger?.LogDebug("SimConnect is not connected. Cannot add request.");
                throw new SimulatorNotConnectedException("SimConnect is not connected. Cannot add request.");
            }

            if (lSimvarRequests is null)
            {
                lSimvarRequests = [];
            }

            if (m_iCurrentDefinition >= (uint)DEFINITION.MAX_DEFINITIONS || m_iCurrentRequest >= (uint)REQUEST.MAX_REQUESTS)
            {
                logger?.LogError("Maximum definitions or requests reached. Cannot add more.");
                return;
            }

            SimvarRequest oSimvarRequest = new()
            {
                eDef = (DEFINITION)m_iCurrentDefinition,
                eRequest = (REQUEST)m_iCurrentRequest,
                sName = _sNewSimvarRequest,
                bIsString = _bIsString,
                sUnits = _bIsString ? null : _sNewUnitRequest
            };

            try
            {
                oSimvarRequest.bPending = !RegisterToSimConnect(oSimvarRequest);
                oSimvarRequest.bStillPending = oSimvarRequest.bPending;
            }
            catch (Exception ex)
            {
                logger?.LogError($"{ex} Failed to register SimvarRequest: {_sNewSimvarRequest}");
                oSimvarRequest.bPending = true;
                oSimvarRequest.bStillPending = true;
            }

            lSimvarRequests.Add(oSimvarRequest);

            lSimvarRequests.Add(oSimvarRequest);

            ++m_iCurrentDefinition;
            ++m_iCurrentRequest;
            logger?.LogDebug($"Request {_sNewSimvarRequest} added with Definition ID: {oSimvarRequest.eDef} and Request ID: {oSimvarRequest.eRequest}");
   }
        private void ReceiveSimConnectMessage()
        {
            try
            {
                m_oSimConnect?.ReceiveMessage();
            }
            catch (Exception ex)
            {
                logger?.LogError( $"{ex} Error receiving SimConnect message.");
                m_oSimConnect = null;
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
                        m_oSimConnect.AddToDataDefinition(_oSimvarRequest.eDef, _oSimvarRequest.sName, "", SIMCONNECT_DATATYPE.STRING256, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                        m_oSimConnect.RegisterDataDefineStruct<SimvarRequest>(_oSimvarRequest.eDef);
                    }
                    else
                    {
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
        private void SimConnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            logger?.LogDebug("SimConnect_OnRecvOpen");
            if (lSimvarRequests is null)
            {
                return;
            }

            foreach (SimvarRequest oSimvarRequest in lSimvarRequests)
            {
                if (oSimvarRequest.bPending)
                {
                    try
                    {
                        oSimvarRequest.bPending = !RegisterToSimConnect(oSimvarRequest);
                        oSimvarRequest.bStillPending = oSimvarRequest.bPending;
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError($"{ex} Error processing SimobjectData for {oSimvarRequest.sName}");
                        oSimvarRequest.bPending = true;
                        oSimvarRequest.bStillPending = true;
                    }
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
            List<Dictionary<string, string>> AircraftData = [];

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
                                    AircraftData.Add(new Dictionary<string, string> { { oSimvarRequest.sName, oSimvarRequest.sValue ?? string.Empty } });
                                    logger?.LogDebug($"Received string value: {oSimvarRequest.sValue} for request: {oSimvarRequest.sName}");
                                }
                                else
                                {
                                    double dValue = (double)data.dwData[0];
                                    oSimvarRequest.dValue = dValue;
                                    oSimvarRequest.sValue = dValue.ToString("F9");
                                    AircraftData.Add(new Dictionary<string, string> { { oSimvarRequest.sName, oSimvarRequest.dValue.ToString() } });
                                    logger?.LogDebug($"Received double value: {dValue} for request: {oSimvarRequest.sName}");
                                }
                            }
                            catch (Exception ex)
                            {
                                logger?.LogError($"{ex} Error processing SimobjectData for {oSimvarRequest.sName}");
                            }

                            oSimvarRequest.bPending = false;
                            oSimvarRequest.bStillPending = false;
                        }
                    }
                }
            }

        }
        public static bool ValidateRequest(string request)
        {
            Console.WriteLine($"Validating request: {request}");

            string trimmedRequest;
            string trimmedIndex;

            if (request?.Split(":").Length < 2)
            {
                // If no index is provided, default to "0"
                trimmedRequest = request ?? "";
                trimmedIndex = "1";
            }
            else
            {
                trimmedRequest = request?.Split(":")[0] ?? "";
                trimmedIndex = request?.Split(":")[1] ?? "1";
            }
            Console.WriteLine($"Checking index : {trimmedIndex} is an integer between 1 and 10");

            if (!int.TryParse(trimmedIndex, out int index))
            {
                Console.WriteLine($"Non integer validating request index: {trimmedIndex}");
                return false;
            }

            if (index < 1 || index > 10)
            {
                Console.WriteLine($"Integer value out of bounds in request index: {index}");
                return false;
            }

            Console.WriteLine($"Checking request: {trimmedRequest} is in SimVar List");
            return !string.IsNullOrWhiteSpace(request) && SimVars.Names.Contains(trimmedRequest);
        }

        #endregion

        #region Public Methods
        /// <summary>  
        /// Retrieves the current aircraft data and connection status from the simulator.  
        /// </summary>  
        /// <returns>A dictionary containing the connection status and aircraft data.</returns>  
        public Dictionary<string, string> AircraftData()
        {
            ReceiveSimConnectMessage();

            Dictionary<string, string> ReturnValue = new()
                {
                    { "IsConnected", false.ToString() },
                    { "AircaftLoaded", AircaftLoaded ?? UnknownAircraft }
                };
            try
            {
                m_oSimConnect?.RequestSystemState(Requests.AIRCRAFT_LOADED, "AircraftLoaded");
            }
            catch (Exception ex)
            {
                logger?.LogError($"{ex} Error requesting system state.");
            }

            if (lSimvarRequests != null)
            {
                foreach (SimvarRequest oSimvarRequest in lSimvarRequests)
                {
                    if (!oSimvarRequest.bPending)
                    {
                        try
                        {
                            m_oSimConnect?.RequestDataOnSimObjectType(oSimvarRequest.eRequest, oSimvarRequest.eDef, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);
                            oSimvarRequest.bPending = true;
                        }
                        catch (Exception ex)
                        {
                            logger?.LogError($"{ex} Error processing SimobjectData for {oSimvarRequest.sName}");
                        }
                    }
                }
            }
            return ReturnValue;
        }

        /// <summary>
        /// Adds a new Simvar request to the SimConnect connection.
        /// </summary>
        /// <param name="_sNewSimvarRequest">The name of the Simvar to request.</param>
        public void AddRequest(string _sNewSimvarRequest)
        {
            try
            {
                InternalAddRequest(_sNewSimvarRequest, "", false);
            }
            catch (Exception ex)
            {
                logger?.LogError($"{ex} Error adding request: {_sNewSimvarRequest}");
                throw; // Fixes CA2200 by re-throwing the exception without altering the stack trace.
            }
        }
        /// <summary>
        /// Adds multiple Simvar requests to the SimConnect connection.
        /// </summary>
        /// <param name="Outputs">A list of Simvar names to request.</param>
        public void AddRequests(List<string> Outputs)
        {
            if (Outputs is not null && Outputs.Count > 0)
            {
                foreach (string output in Outputs)
                {
                    try
                    {
                        AddRequest(output);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError($"{ex.Message} Error adding request:{output}" );
                    }
                }
            }
        }
        public Connect( DisplayLogging logger )
        {
            this.logger = logger;
            this.Initialise(IntPtr.Zero, DefaultTimerIntervalMs);
        }
 
        public Connect(IntPtr hWnd, int time)
        {
            this.Initialise(hWnd, time);
        }

        private void Initialise(IntPtr hWnd, int time)
        {
            try
            {

                if (hWnd != IntPtr.Zero)
                {
                    this.hWnd = hWnd;
                }

                lSimvarRequests = [];
            }
            catch (Exception ex)
            {
                logger?.LogError(ex.Message);
            }
        }
        #endregion

    }
}