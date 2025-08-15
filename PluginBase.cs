using BroadcastPluginSDK;
using System.Diagnostics;
using System.Reflection;

namespace MSFSPlugin
{
    public class PluginBase : BroadcastPlugin
    {
        public override string Stanza => "MSFS";
        public override void Start()
        {
            Debug.WriteLine($"Starting {Name} plugin.");
            // This PluginBase does not do anything on start, but it could be used to initialize resources or connections.
        }

        public PluginBase() : base()
        {
            Name = "MFSF PluginBase";
            Description = "Microsoft Flight Simulator PluginBase.";
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
            Icon = Properties.Resources.red;
        }
    }
}
