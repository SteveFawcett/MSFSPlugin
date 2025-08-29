using BroadcastPluginSDK.Interfaces;
using Microsoft.Extensions.Logging;
using MSFSPlugin.Controls;
using System.Diagnostics;

namespace MSFSPlugin.Forms
{
    public partial class DisplayLogging : UserControl
    {

        public void LogInformation( string value) { MsgTxtBox.LogInfo(value); }
        public void LogDebug(string value) { MsgTxtBox.LogDebug(value); }

        public void LogError(string value) { MsgTxtBox.LogError(value); }

        public void LogWarning(string value) { MsgTxtBox.LogWarning(value); }

        ILogger<IPlugin> _logger;

        public DisplayLogging( ILogger<IPlugin> logger)
        {
            _logger = logger;
            InitializeComponent();
        }
    }

    public static class WinFormsExtensions
    {
        public static void AppendLine(this RichTextBox source, string value)
        {

            if (source.InvokeRequired)
            {
                source.Invoke(new Action(() => source.AppendText(value + Environment.NewLine)));
            }
            else
            {
                source.AppendText(value + Environment.NewLine);
            }
        }
    }
}
