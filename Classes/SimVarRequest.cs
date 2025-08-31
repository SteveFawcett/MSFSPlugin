using Microsoft.FlightSimulator.SimConnect;
using System.Text;

namespace MSFSPlugin.Models
{
    public enum REQUEST
    {  
        Dummy = 0, 
        ResultStructure,
        MAX_REQUESTS = 100
    };

    public enum DEFINITION
    {
        Dummy = 0,
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


}
