using System.Runtime.InteropServices;

namespace MSFSPlugin.Classes
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct ResultStructure
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string sValue;
    };

    /// <summary>
    /// Represents a request for a simulation variable.
    /// </summary>
    internal class SimvarRequest
    {
        /// <summary>
        /// Gets or sets the definition of the simulation variable.
        /// </summary>
        public DEFINITION eDef = DEFINITION.Dummy;

        /// <summary>
        /// Gets or sets the request type for the simulation variable.
        /// </summary>
        public REQUEST eRequest = REQUEST.Dummy;

        /// <summary>
        /// Gets or sets the name of the simulation variable.
        /// </summary>
        public string? sName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the simulation variable is a string.
        /// </summary>
        public bool bIsString { get; set; }

        /// <summary>
        /// Gets or sets the numeric value of the simulation variable.
        /// </summary>
        public double dValue
        {
            set { m_dValue = value; }
            get { return m_dValue; }
        }
        private double m_dValue = 0.0;

        /// <summary>
        /// Gets or sets the string value of the simulation variable.
        /// </summary>
        public string? sValue
        {
            set { m_sValue = value; }
            get => m_sValue;
        }
        private string? m_sValue = null;

        /// <summary>
        /// Gets or sets the units of the simulation variable.
        /// </summary>
        public string? sUnits { get; set; }

    }
    /// <summary>  
    /// This class is used to pass data from the simulator to the listener.  
    /// </summary>  
    public class SimulatorData : EventArgs
    {
        /// <summary>  
        /// Gets or sets the time when the data was received.  
        /// </summary>  
        public DateTime TimeReached { get; set; }

        /// <summary>  
        /// Gets or sets the aircraft data as a list of dictionaries containing key-value pairs.  
        /// </summary>  
        public List<Dictionary<string, string>> AircraftData { get; set; } = new List<Dictionary<string, string>>();
    }
}
