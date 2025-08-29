using Microsoft.Extensions.Configuration;
using MSFSPlugin.Forms;
using System.Reflection;

namespace MSFSPlugin.Classes
{
    internal class SimSdkLoader
    {
        private static object? simConnect;
        private static Type? simConnectType;
        private static Type? simConnectDatatypeEnum;
        private static Assembly? simConnectAssembly;

        static public void Load( DisplayLogging log,  IConfiguration configuration)
        {
            log.LogInformation("Loading SimConnect SDK...");

            var sdkPath = Helpers.GetConfiguration(configuration, "SDK", "C:\\MSFS 2024 SDK\\SimConnect SDK");
            

            AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
            {
                if (args.Name.StartsWith("Microsoft.FlightSimulator.SimConnect"))
                {
                    string dllPath = Path.Combine(sdkPath, "lib", "managed", "Microsoft.FlightSimulator.SimConnect.dll");
                    simConnectAssembly = Assembly.LoadFrom(dllPath);
                    simConnectDatatypeEnum = simConnectAssembly.GetType("Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATATYPE");
                    simConnectType = simConnectAssembly.GetType("Microsoft.FlightSimulator.SimConnect.SimConnect");
                    return simConnectAssembly;
                }
                return null;
            };

            NativeLoader.LoadEmbeddedDll(
                "MSFSPlugin.SimConnect.SimConnect.dll",
                Path.Combine(sdkPath, "lib"),
                "SimConnect.dll"
            );

            log.LogInformation("SimConnect SDK loaded.");
        }
    }
}
