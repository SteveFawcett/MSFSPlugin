using BroadcastPluginSDK.abstracts;
using BroadcastPluginSDK.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MSFSPlugin.Classes;
using MSFSPlugin.Controls;
using MSFSPlugin.Forms;
using MSFSPlugin.Properties;
using System.Configuration;
using System.Data;
using System.Runtime.CompilerServices;
using System.Timers;

namespace MSFSPlugin;

public partial class PluginBase : BroadcastPluginBase, IProvider, IManager, IDisposable
{
    private const string STANZA = "MSFS";
    private ILogger<IPlugin>? _logger;
    private DisplayLogging? _displayLogging;
    private FlightSimulator? connect;
    private IConfiguration? _configuration;
    private System.Timers.Timer? connectTimer;
    private bool isConnected = false;
    private bool disposedValue;
    private readonly object connectionLock = new();
    public event EventHandler<Dictionary<string, string>>? DataReceived;

    public PluginBase() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginBase"/> class.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="logger">The logger instance.</param>
    public PluginBase(IConfiguration configuration, ILogger<IPlugin> logger) :
        base(configuration, null, Resources.red, STANZA)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _displayLogging = new DisplayLogging(logger);
        _configuration = configuration;

        _displayLogging.LogInformation("MSFS Plugin Initialized");
        _displayLogging.LogInformation($"Process is {(Environment.Is64BitProcess ? "64-bit" : "32-bit")}");

        SimSdkLoader.Load(_displayLogging, configuration); // Load SimConnect SDK

        SetMenu(); // Setup context menu

        var messageWindow = new SimConnectMessageWindow();
        messageWindow.OnSimConnectMessage += (msg) =>
        {
            try
            {
                logger?.LogInformation("Received SimConnect message.");
                connect?.Connection?.ReceiveMessage();
            }
            catch (Exception ex)
            {
                logger?.LogError($"ReceiveMessage failed: {ex.Message}");
            }
        };

        connect = new FlightSimulator(_displayLogging, messageWindow.Handle);
        connect.ConnectionStatusChanged += Connector_ConnectionStatusChanged;
        connect.DataReceived += FlightSimulator_DataReceived;
        SetupTimer(); // Setup connection timer

    }

    private void FlightSimulator_DataReceived(object? sender, Dictionary<string, object> e)
    {
        var stringData = e.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString() ?? string.Empty);
        DataReceived?.Invoke(this, stringData);
    }

    private void SetupTimer()
    {
        connectTimer = new System.Timers.Timer(3000); // every 3 seconds
        connectTimer.Elapsed += OnTimerElapsed;
        connectTimer.AutoReset = true;
        connectTimer.Start();
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if( connect is not null && connect.isConnected ) return; // Already connected

        SimulatorReconnect();        
    }

    private void SimulatorReconnect()
    {
        lock (connectionLock)
        {
            if (connect == null) return;

            try
            {
                _displayLogging?.LogDebug("SimulatorReconnect() was called by timer.");
                if (isConnected) return; // Already connected
                _displayLogging?.LogInformation("Attempting to connect to Flight Simulator...");
                isConnected = connect.ConnectToSim();
            }
            catch (Exception ex)
            {
                _displayLogging?.LogError($"Error during Simulator Reconnect: {ex.Message}");
            }
        }
    }

    private void Connector_ConnectionStatusChanged(object? sender, bool newConnectionStatus)
    {
        lock (connectionLock)
        {
            if( isConnected == newConnectionStatus ) return;

            isConnected = newConnectionStatus;
            if (isConnected)
            {
                _displayLogging?.LogInformation("Connected to Flight Simulator");
                Icon = Resources.green; //TODO: Trigger an Icon change
                LoadExportedVariables();
            }
            else
            {
                _displayLogging?.LogWarning("Disconnected from Flight Simulator");
                Icon = Resources.red;
            }
        }
    }

    private void LoadExportedVariables()
    {
        if( _configuration is null ) return;

        foreach (var config in _configuration.GetSection("MSFS").GetChildren())
            if (config.Key == "export")
                foreach (var dataSet in config.GetChildren())
                {
                    connect?.AddRequest(dataSet["variable"] ?? "Not Known", dataSet["measure"] ?? "feet" ) ;
                }
                   
    }
    /// <summary>
    /// Releases the unmanaged resources used by the PluginBase and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                if (connectTimer != null)
                {
                    connectTimer.Elapsed -= OnTimerElapsed;
                    connectTimer.Stop();
                    connectTimer.Dispose();
                    connectTimer = null;
                }
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

}