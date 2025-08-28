using BroadcastPluginSDK.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MSFSPlugin.Classes
{
    public class FlightSimConnector : IDisposable
    {
        IntPtr simConnectHandle;

        private SimConnect? simConnect;
        private nint windowHandle ;
        private const int WM_USER_SIMCONNECT = 0x0402;

        public event EventHandler<bool>? ConnectionStatusChanged;

        private bool isConnected = false;

        public FlightSimConnector(nint hwnd)
        {
            windowHandle = hwnd;
        }

        public FlightSimConnector(IConfiguration configuration, ILogger<IPlugin> logger)
        {
            logger.LogInformation( "Flight Simulator Connector Starting" );

            var SDK = configuration.GetValue<string>("SDK") ?? string.Empty;
            if (string.IsNullOrEmpty(SDK))
            {
                SDK = "C:\\MSFS 2024 SDK\\SimConnect SDK";
                configuration["SDK"] = SDK;
            }

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                if (args.Name.StartsWith("Microsoft.FlightSimulator.SimConnect"))
                {
                    return EmbeddedAssemblyLoader.LoadManagedAssembly(
                        Path.Combine( SDK , "lib\\managed" ),
                        "Microsoft.FlightSimulator.SimConnect.dll"
                    );
                }

                return null;
            };
         
            simConnectHandle = NativeLoader.LoadEmbeddedDll("MSFSPlugin.SimConnect.SimConnect.dll",
                                                            Path.Combine( SDK , "lib" ), 
                                                            "SimConnect.dll");

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
            }
            catch (COMException)
            {
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
            }
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
