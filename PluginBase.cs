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

public partial class PluginBase : BroadcastPluginBase, IManager
{
    private const string STANZA = "MSFS";
    private ILogger<IPlugin>? _logger;
    private DisplayLogging? _displayLogging;
    private Connect? connect;
    private System.Timers.Timer? connectTimer;
    private bool isConnected = false;


    public PluginBase() : base() { }

    public PluginBase(IConfiguration configuration, ILogger<IPlugin> logger) :
        base(configuration, null, Resources.red, STANZA)
    {

        _logger = logger;
        _displayLogging = new DisplayLogging(logger);

        _displayLogging.LogInformation("MSFS Plugin Initialized");
        _displayLogging.LogInformation($"Process is {(Environment.Is64BitProcess ? "64-bit" : "32-bit")}");

        SimSdkLoader.Load(_displayLogging, configuration); // Load SimConnect SDK

        setMenu(); // Setup context menu

        connect = new Connect(_displayLogging);
        connect.ConnectionStatusChanged += Connector_ConnectionStatusChanged;

        SetupTimer(); // Setup connection timer

        //connect.AddRequests(["PLANE ALTITUDE"]);
    }

    private void SetupTimer()
    {
        connectTimer = new System.Timers.Timer(3000); // every 3 seconds
        connectTimer.Elapsed += (_, _) => SimulatorReconnect();
        connectTimer.AutoReset = true;
        connectTimer.Start();
    }

    private void SimulatorReconnect()
    {
        if (isConnected || connect is null ) return;
       
        _displayLogging?.LogDebug("SimulatorReconnect() was called by timer.");
        isConnected = connect.ConnectToSim();
    }

    private void Connector_ConnectionStatusChanged(object? sender, bool isConnected)
    {
        if (isConnected)
        {
            _displayLogging?.LogInformation(  "Connected to Flight Simulator");
            Icon = Resources.green;
        }
        else
        {
            _displayLogging?.LogWarning("Disconnected from Flight Simulator") ;
            Icon = Resources.red;
        }
    }
}