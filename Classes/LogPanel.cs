using System;
using System.Drawing;
using System.Windows.Forms;

// TODO: Move this into a tools assembly

namespace MSFSPlugin.Controls
{
    public class LogPanel : UserControl
    {

        private void InitializeComponent()
        {
            SuspendLayout();
            // 
            // LogPanel
            // 
            Name = "LogPanel";
            Size = new Size(969, 316);
            ResumeLayout(false);
        }


        public LogPanel()
        {
            showDebug = new CheckBox();
            showDebug.Checked = true;
            showDebug.Dock = DockStyle.Bottom;
            showDebug.AutoSize = true;
            showDebug.Padding = new Padding(9, 3, 0, 3); // Optional: adds spacing
            showDebug.Name = "showDebug";
            showDebug.Text = "Show Debug";
            showDebug.UseVisualStyleBackColor = true;

            logBox = new RichTextBox();
            logBox.Dock = DockStyle.Fill;
            logBox.BackColor = Color.WhiteSmoke;
            logBox.BorderStyle = BorderStyle.None;
            logBox.Font = new Font("Consolas", 10F);
            logBox.HideSelection = false;
            logBox.ReadOnly = true;
            logBox.Name = "logBox";
            logBox.Text = "";

            InitializeComponent();

            this.Controls.Add(showDebug);
            this.Controls.Add(logBox);
        }

        private readonly Dictionary<string, Color> _logColors = new()
        {
            { "INFO", Color.Green },
            { "WARN", ColorTranslator.FromHtml("#FFBF00") },
            { "ERROR", Color.OrangeRed },
            { "DEBUG", Color.Blue }
        };

        public void LogInformation(string message) => AppendLog(message, _logColors["INFO"], "INFO");
        public void LogWarning(string message) => AppendLog(message, _logColors["WARN"], "WARN");
        public void LogError(string message) => AppendLog(message, _logColors["ERROR"], "ERROR");
        public void LogDebug(string message) => AppendLog(message, _logColors["DEBUG"], "DEBUG");

        public void ClearLog() => logBox.Clear();

        private void AppendLog(string message, Color color, string level)
        {
            if (logBox.InvokeRequired)
            {
                logBox.Invoke(new Action(() => AppendLogInternal(message, color, level)));
            }
            else
            {
                AppendLogInternal(message, color, level);
            }
        }

        private void AppendLogInternal(string message, Color color, string level)
        {
            if (level == "DEBUG" && !showDebug.Checked)
            {
                return; // Skip debug messages if the checkbox is not checked
            }
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string prefix = $"[{timestamp}] [{level}] ";

            int start = logBox.TextLength;
            logBox.SelectionStart = start;
            logBox.SelectionLength = 0;

            logBox.SelectionColor = Color.Gray;
            logBox.AppendText(prefix);

            logBox.SelectionColor = color;
            logBox.AppendText(message + Environment.NewLine);

            logBox.SelectionColor = logBox.ForeColor;
            logBox.ScrollToCaret();
        }
        private CheckBox showDebug;
        private RichTextBox logBox;
    }
}
