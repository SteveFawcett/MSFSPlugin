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
        INT32 = 101,
        INT64 = 102,
        FLOAT32 = 103,
        FLOAT64 = 104,
        STRING8 = 105,
        STRING32 = 106,
        STRING64 = 107,
        STRING128 = 108,
        STRING256 = 109
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
