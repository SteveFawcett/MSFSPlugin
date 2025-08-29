using BroadcastPluginSDK.abstracts;
using BroadcastPluginSDK.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MSFSPlugin.Classes;
using MSFSPlugin.Forms;
using MSFSPlugin.Properties;
using System.Runtime.CompilerServices;
using System.Timers;

namespace MSFSPlugin;

public partial class PluginBase : BroadcastPluginBase, IManager, IDisposable
{
    private const string STANZA = "MSFS";
    private ILogger<IPlugin>? _logger;
    private DisplayLogging? _displayLogging;
    private FlightSimulator? connect;
    private System.Timers.Timer? connectTimer;
    private bool isConnected = false;
    private bool disposedValue;
    private readonly object connectionLock = new();

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

        _displayLogging.LogInformation("MSFS Plugin Initialized");
        _displayLogging.LogInformation($"Process is {(Environment.Is64BitProcess ? "64-bit" : "32-bit")}");

        SimSdkLoader.Load(_displayLogging, configuration); // Load SimConnect SDK

        SetMenu(); // Setup context menu

        connect = new FlightSimulator(_displayLogging);
        connect.ConnectionStatusChanged += Connector_ConnectionStatusChanged;

        SetupTimer(); // Setup connection timer

        //connect.AddRequests(["PLANE ALTITUDE"]);
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
        SimulatorReconnect();
    }

    private void SimulatorReconnect()
    {
        lock (connectionLock)
        {
            if (isConnected || connect is null) return;

            try
            {
                _displayLogging?.LogDebug("SimulatorReconnect() was called by timer.");
                isConnected = connect.ConnectToSim();
            }
            catch (Exception ex)
            {
                _displayLogging?.LogError($"Error during SimulatorReconnect: {ex.Message}");
            }
        }
    }

    private void Connector_ConnectionStatusChanged(object? sender, bool newConnectionStatus)
    {
        lock (connectionLock)
        {
            isConnected = newConnectionStatus;
            if (isConnected)
            {
                _displayLogging?.LogInformation("Connected to Flight Simulator");
                Icon = Resources.green;
            }
            else
            {
                _displayLogging?.LogWarning("Disconnected from Flight Simulator");
                Icon = Resources.red;
            }
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