using Microsoft.Extensions.Logging;
using MSFSPlugin.Forms;

namespace MSFSPlugin
{
    partial class PluginBase
    {
        public bool Locked { get; set; } = false; // This plugin does not require locking functionality
        public List<ToolStripItem>? ContextMenuItems { get; set; } = null;

        //Ignore this warning - we are using nullable events
#pragma warning disable CS0067
        public event EventHandler<bool>? TriggerRestart;   // Not implemented in this plugin
        public event EventHandler<UserControl>? ShowScreen; // Not implemented in this plugin
        public event EventHandler? WriteConfiguration;     // Not implemented in this plugin
#pragma warning restore CS0067

        private void SetMenu()
        {
            ContextMenuItems = new List<ToolStripItem>()
            {
                new ToolStripMenuItem("Open"         , null, OnOpenClicked),
                new ToolStripMenuItem("Messages"     , null, OnMessagesClicked),
            };
        }

        private void OnOpenClicked(object? sender, EventArgs e)
        {
            if (InfoPage != null)
                if(InfoPage is UserControl uc)
                    ShowScreen?.Invoke(this, uc);
        }

        private void OnMessagesClicked(object? sender, EventArgs e)
        {
            if (_displayLogging != null)
                ShowScreen?.Invoke(this, _displayLogging);
        }
    }
}
