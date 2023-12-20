using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace SindenCompanion
{
    public class ForegroundProcess
    {
        public readonly uint ProcessId;
        public readonly string ProcessName;

        public readonly string WindowTitle;

        public ForegroundProcess()
        {
            const int nChars = 256;
            var buff = new StringBuilder(nChars);
            var handle = GetForegroundWindow();

            if (GetWindowText(handle, buff, nChars) > 0) WindowTitle = buff.ToString();

            GetWindowThreadProcessId(handle, out ProcessId);
            var process = Process.GetProcessById((int)ProcessId);
            ProcessName = process.ProcessName;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    }
}