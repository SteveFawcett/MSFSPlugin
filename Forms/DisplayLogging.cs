using BroadcastPluginSDK.Interfaces;
using CyberDog.Controls;
using Microsoft.Extensions.Logging;

namespace MSFSPlugin.Forms
{
    public partial class DisplayLogging : LogPanel
    {
        ILogger<IPlugin>? _logger;

        private void InitializeComponent()
        {
            SuspendLayout();
            // 
            // DisplayLogging
            // 
            Name = "DisplayLogging";
            Size = new Size(703, 348);
            ResumeLayout(false);

        }

        public DisplayLogging(ILogger<IPlugin>? logger = null) : base()
        {
            _logger = logger;
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
