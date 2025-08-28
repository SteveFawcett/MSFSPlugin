using BroadcastPluginSDK.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.FlightSimulator.SimConnect;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MSFSPlugin.Classes
{
    public class FlightSimConnector : IDisposable
    {
        private IntPtr simConnectHandle;
        private SimConnect? simConnect;
        private readonly nint windowHandle;
        private const int WM_USER_SIMCONNECT = 0x0402;

        private readonly ILogger<IPlugin>? logger;
        private readonly System.Timers.Timer? connectionRetryTimer;

        private bool isConnected = false;
        private int period;
        public event EventHandler<bool>? ConnectionStatusChanged;

        public FlightSimConnector(nint hwnd)
        {
            windowHandle = hwnd;
        }

        public FlightSimConnector(IConfiguration configuration, ILogger<IPlugin> logger)
        {
            this.logger = logger;
            logger.LogInformation("Flight Simulator Connector Starting");

            var SDK = Helpers.GetConfiguration(configuration, "SDK", "C:\\MSFS 2024 SDK\\SimConnect SDK");
            string PERIOD = Helpers.GetConfiguration(configuration, "PERIOD", "5000");

            if (!int.TryParse(PERIOD, out period))
            {
                period = 5000; // Default to 5 seconds if parsing fails
            }

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                if (args.Name.StartsWith("Microsoft.FlightSimulator.SimConnect"))
                {
                    string dllPath = Path.Combine(SDK, "lib", "managed", "Microsoft.FlightSimulator.SimConnect.dll");
                    return Assembly.LoadFrom(dllPath);
                }
                return null;
            };

            simConnectHandle = NativeLoader.LoadEmbeddedDll(
                "MSFSPlugin.SimConnect.SimConnect.dll",
                Path.Combine(SDK, "lib"),
                "SimConnect.dll"
            );

            // Set up retry timer
            logger?.LogInformation($"Setting up connection retry timer with period: {period} ms");
            connectionRetryTimer = new System.Timers.Timer(period);
            connectionRetryTimer.Elapsed += (s, e) => TryConnect();
            connectionRetryTimer.AutoReset = true;
            connectionRetryTimer.Start();
        }

        private void TryConnect()
        {
            if (isConnected || simConnect != null) return;

            logger?.LogDebug("Attempting to connect to SimConnect...");
            Connect();
        }

        public void Connect()
        {
            try
            {
                simConnect = new SimConnect("FlightSimConnector", windowHandle, WM_USER_SIMCONNECT, null, 0);
                simConnect.OnRecvOpen += SimConnect_OnRecvOpen;
                simConnect.OnRecvQuit += SimConnect_OnRecvQuit;
                simConnect.OnRecvException += SimConnect_OnRecvException;

                UpdateConnectionStatus(true);
                logger?.LogInformation("SimConnect connection established.");
            }
            catch (COMException)
            {
                logger?.LogWarning( "SimConnect connection attempt failed.");
                UpdateConnectionStatus(false);
            }
        }

        public void Disconnect()
        {
            if (simConnect != null)
            {
                simConnect.Dispose();
                simConnect = null;
                UpdateConnectionStatus(false);
                logger?.LogInformation("SimConnect disconnected.");
            }
        }

        public void HandleSimConnectMessage()
        {
            simConnect?.ReceiveMessage();
        }

        private void SimConnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            UpdateConnectionStatus(true);
        }

        private void SimConnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            UpdateConnectionStatus(false);
        }

        private void SimConnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            UpdateConnectionStatus(false);
        }

        private void UpdateConnectionStatus(bool connected)
        {
            if (isConnected != connected)
            {
                isConnected = connected;
                ConnectionStatusChanged?.Invoke(this, connected);
                logger?.LogInformation($"SimConnect status changed: {(connected ? "Connected" : "Disconnected")}");
            }
        }

        public void Dispose()
        {
            connectionRetryTimer?.Stop();
            connectionRetryTimer?.Dispose();
            Disconnect();
        }
    }
}
