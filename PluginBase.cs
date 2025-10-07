using BroadcastPluginSDK.abstracts;
using BroadcastPluginSDK.Classes;
using BroadcastPluginSDK.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MSFSPlugin.Classes;
using MSFSPlugin.Forms;
using MSFSPlugin.Properties;
using System.Timers;

namespace MSFSPlugin;

public partial class MSFSPlugin : BroadcastPluginBase, IProvider, IManager, ICommandHandler ,  IDisposable
{
    private const string STANZA = "MSFS";
    private ILogger<IPlugin>? _logger;
    private DisplayLogging? _displayLogging;
    private FlightSimulator? connect;
    private IConfiguration? _configuration;
    private bool isConnected = false;
    private bool disposedValue;

    public event EventHandler<CacheData>? DataReceived;
    public event EventHandler<CommandItem>? CommandReceived;

    public MSFSPlugin() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MSFSPlugin"/> class.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="logger">The logger instance.</param>
    public MSFSPlugin(IConfiguration configuration, ILogger<IPlugin> logger) :
        base(configuration, null, Resources.red, STANZA)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _displayLogging = new DisplayLogging(logger);
        _configuration = configuration;

        _displayLogging.LogInformation("MSFS Plugin Initialized");
        _displayLogging.LogInformation($"Process is {(Environment.Is64BitProcess ? "64-bit" : "32-bit")}");

        SimSdkLoader.Load(_displayLogging, configuration); // Load SimConnect SDK

        SetMenu(); // Setup context menu

        connect = new FlightSimulator(_displayLogging);
        connect.ConnectionStatusChanged += Connector_ConnectionStatusChanged;
        connect.DataReceived += FlightSimulator_DataReceived;
    }

    public void SetState(bool isConnected = false)
    {
        base.Icon = isConnected ? Resources.green : Resources.red;

        ImageChangedInvoke(base.Icon);

    }
    private void FlightSimulator_DataReceived(object? sender, CacheData e)
    {
        DataReceived?.Invoke(this, e);
    }

    private void Connector_ConnectionStatusChanged(object? sender, bool newConnectionStatus)
    {
        if ( isConnected == newConnectionStatus ) return;

        isConnected = newConnectionStatus;
        if (isConnected)
        {
            _displayLogging?.LogInformation("Connected to Flight Simulator");
            LoadExportedVariables();
            SetState(isConnected);

        }
        else
        {
            _displayLogging?.LogWarning("Disconnected from Flight Simulator");
            SetState(isConnected);
        }
}

    private void LoadExportedVariables()
    {
        if (_configuration is null) return;

        foreach (var config in _configuration.GetSection("MSFS").GetChildren())
            if (config.Key == "export")
                foreach (var dataSet in config.GetChildren())
                {
                    connect?.AddRequest(dataSet["variable"] ?? string.Empty ) ;
                }
                   
    }
    /// <summary>
    /// Releases the unmanaged resources used by the MSFSPlugin and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                if (connect != null)
                {
                    connect.Dispose();
                    connect = null;
                }
                if (_displayLogging != null)
                {
                    _displayLogging.Dispose();
                    _displayLogging = null;
                }
            }
            disposedValue = true;
        }
    }


    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public void CommandHandler(CommandItem cmd)
    {
        if( cmd.CommandType != CommandTypes.Simulator)
        {
            _logger?.LogInformation($"Received unsupported command type: {cmd.CommandType}");
            return;
        }
    }
}