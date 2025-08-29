namespace MSFSPlugin.Classes
{
    internal enum DATA_DEFINE_ID
    {
        AIRCRAFT_POSITION = 101,
    };

    /// <summary>  
    /// Enumeration for SimConnect definitions.  
    /// </summary>  
    internal enum DEFINITION
    {
        /// <summary>  
        /// Dummy definition.  
        /// </summary>  
        Dummy = 0,

        /// <summary>  
        /// Maximum number of definitions.  
        /// </summary>  
        MAX_DEFINITIONS = 100
    };
    /// <summary>  
    /// Enumeration for SimConnect requests.  
    /// </summary>  
    internal enum REQUEST
    {
        /// <summary>  
        /// Dummy request.  
        /// </summary>  
        Dummy = 0,

        /// <summary>  
        /// Request for result structure.  
        /// </summary>  
        ResultStructure,

        /// <summary>  
        /// Maximum number of requests.  
        /// </summary>  
        MAX_REQUESTS = 100
    };
    internal enum Event
    {
        RECUR_1SEC,
    }
    internal enum Requests
    {
        SIMULATION,
        AIRCRAFT_LOADED,
    }

    public partial class Connect 
    {
        /// <summary> Constants for SimConnect messages
        /// These constants are used to identify messages sent by SimConnect.
        /// They are used in the message loop to process incoming SimConnect messages.
        /// </summary>   

        private const uint WM_USER_SIMCONNECT = 0x0402;
        private const int DefaultTimerIntervalMs = 30000;  // Time to check 30 Seconds , I dont want it too slow or two often
        private const string UnknownAircraft = "Unknown";
    }
}
