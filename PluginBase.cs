using BroadcastPluginSDK;
using Microsoft.Extensions.Configuration;


namespace MSFSPlugin
{
    public class PluginBase : BroadcastPluginBase
    {
        public PluginBase( IConfiguration configuration) : base(
            configuration , null,
            Properties.Resources.red,
            "FlightSim",
            "MSFS",
            "Microsoft Flight Simulator PluginBase.")
        {
        }
    }
}
