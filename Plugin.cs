using Microsoft.Extensions.Configuration;
using PluginBase;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace MSFSPlugin
{
    public class Plugin : BroadcastPlugin
    {
        public override string Stanza => "MSFS";
        public override void Start()
        {
            Debug.WriteLine($"Starting {Name} plugin.");
            // This Plugin does not do anything on start, but it could be used to initialize resources or connections.
        }

        public Plugin() : base()
        {
            Name = "MFSF Plugin";
            Description = "Microsoft Flight Simulator Plugin.";
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
            Icon = Properties.Resources.red;
        }
    }
}
