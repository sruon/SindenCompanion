using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SindenCompanion
{
    public class ForegroundProcess
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public readonly string WindowTitle;
        public readonly uint ProcessId = 0;
        public readonly string ProcessName;

        public ForegroundProcess()
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                WindowTitle = Buff.ToString();
            }

            GetWindowThreadProcessId(handle, out ProcessId);
            Process process = Process.GetProcessById((int)ProcessId);
            ProcessName = process.ProcessName;
        }
    }


}
