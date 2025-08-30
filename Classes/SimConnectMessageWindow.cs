using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSFSPlugin.Classes
{
    public class SimConnectMessageWindow : NativeWindow
    {
        public event Action<Message>? OnSimConnectMessage;

        private const int WM_USER_SIMCONNECT = 0x0402;

        public SimConnectMessageWindow()
        {
            CreateParams cp = new()
            {
                Caption = "SimConnectMessageWindow",
                ClassName = null,
                X = 0,
                Y = 0,
                Height = 0,
                Width = 0,
                Style = 0x800000, // WS_OVERLAPPED
            };
            CreateHandle(cp);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_USER_SIMCONNECT)
            {
                OnSimConnectMessage?.Invoke(m);
            }

            base.WndProc(ref m);
        }
    }
}
