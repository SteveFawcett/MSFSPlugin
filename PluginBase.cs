using BroadcastPluginSDK.abstracts;
using BroadcastPluginSDK.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MSFSPlugin.Classes;
using MSFSPlugin.Properties;

namespace MSFSPlugin;

public class PluginBase : BroadcastPluginBase
{

    private const string STANZA = "MSFS";
    private ILogger<IPlugin>? _logger;
    private FlightSimConnector? _connector;

    public PluginBase() : base() { }

    public PluginBase(IConfiguration configuration, ILogger<IPlugin> logger) : 
        base( configuration, null, Resources.red, STANZA)
    {
        _connector = new FlightSimConnector(configuration.GetSection(STANZA), logger);
        _logger = logger;

        _logger.LogInformation("MSFS Plugin Initialized");
        _logger.LogInformation($"Process is {(Environment.Is64BitProcess ? "64-bit" : "32-bit")}");

        _connector.ConnectionStatusChanged += Connector_ConnectionStatusChanged;
         _connector.Connect();
    }

    private void Connector_ConnectionStatusChanged(object? sender, bool isConnected)
    {
        if (isConnected)
        {
            _logger?.LogInformation("Connected to Flight Simulator");
            Icon = Resources.green;
        }
        else
        {
            _logger?.LogWarning("Disconnected from Flight Simulator");
            Icon = Resources.red;
        }
    }
}