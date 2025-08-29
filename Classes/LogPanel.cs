using System;
using System.Drawing;
using System.Windows.Forms;


namespace MSFSPlugin.Controls
{
    public class LogPanel : UserControl
    {
        private readonly RichTextBox _logBox;

        public LogPanel()
        {
            _logBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.Black,
                ForeColor = Color.White,
                Font = new Font("Consolas", 10),
                HideSelection = false
            };

            Controls.Add(_logBox);
        }

        private readonly Dictionary<string, Color> _logColors = new()
        {
            { "INFO", Color.Green },
            { "WARN", ColorTranslator.FromHtml("#FFBF00") },
            { "ERROR", Color.OrangeRed },
            { "DEBUG", Color.Blue }
        };

        public void LogInfo(string message) => AppendLog(message, _logColors["INFO"], "INFO");
        public void LogWarning(string message) => AppendLog(message, _logColors["WARN"], "WARN");
        public void LogError(string message) => AppendLog(message, _logColors["ERROR"], "ERROR");
        public void LogDebug(string message) => AppendLog(message, _logColors["DEBUG"], "DEBUG");

        public void ClearLog() => _logBox.Clear();

        private void AppendLog(string message, Color color, string level)
        {
            if (_logBox.InvokeRequired)
            {
                _logBox.Invoke(new Action(() => AppendLogInternal(message, color, level)));
            }
            else
            {
                AppendLogInternal(message, color, level);
            }
        }

        private void AppendLogInternal(string message, Color color, string level)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string prefix = $"[{timestamp}] [{level}] ";

            int start = _logBox.TextLength;
            _logBox.SelectionStart = start;
            _logBox.SelectionLength = 0;

            _logBox.SelectionColor = Color.Gray;
            _logBox.AppendText(prefix);

            _logBox.SelectionColor = color;
            _logBox.AppendText(message + Environment.NewLine);

            _logBox.SelectionColor = _logBox.ForeColor;
            _logBox.ScrollToCaret();
        }
    }
}
