using BroadcastPluginSDK.abstracts;
using Microsoft.Extensions.Configuration;
using MSFSPlugin.Properties;

namespace MSFSPlugin;

public class PluginBase : BroadcastPluginBase
{
    public PluginBase(IConfiguration configuration) : base(
        configuration, null,
        Resources.red,
        "FlightSim",
        "MSFS",
        "Microsoft Flight Simulator PluginBase.")
    {
    }
}