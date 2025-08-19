using BroadcastPluginSDK.abstracts;
using BroadcastPluginSDK.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MSFSPlugin.Properties;

namespace MSFSPlugin;

public class PluginBase : BroadcastPluginBase
{
    private const string PluginName = "MSFSPlugin";
    private const string PluginDescription = "Microsoft Flight Simulator PluginBase.";
    private const string Stanza = "MSFS";

    private ILogger<IPlugin> _logger;

    public PluginBase(IConfiguration configuration, ILogger<IPlugin> logger) : base(
        configuration, null,
        Resources.red,
        PluginName,
        Stanza,
        PluginDescription)
    {
        _logger = logger;
        _logger.LogInformation( PluginDescription );
    }
}