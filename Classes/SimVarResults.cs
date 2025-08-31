using Microsoft.FlightSimulator.SimConnect;
using MSFSPlugin.Models;
using System.Runtime.InteropServices;

namespace MSFSPlugin.Classes
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SimVarStruct { public double Value; }


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi , Pack = 1)]
    public struct SimVarStringStruct
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Value;
    }
}

