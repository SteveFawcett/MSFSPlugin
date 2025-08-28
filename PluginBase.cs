using BroadcastPluginSDK.abstracts;
using BroadcastPluginSDK.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MSFSPlugin.Properties;

namespace MSFSPlugin;

public class PluginBase : BroadcastPluginBase
{

    private const string STANZA = "MSFS";
    private ILogger<IPlugin> _logger;

    public PluginBase(IConfiguration configuration, ILogger<IPlugin> logger) : 
        base( configuration, null, Resources.red, STANZA)
    {
        _logger = logger;
    }
}