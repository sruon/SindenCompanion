using System;
using System.Runtime.InteropServices;

namespace Memory
{
    public class Imps
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(
            uint dwDesiredAccess,
            bool bInheritHandle,
            int dwProcessId
        );


        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] byte[] lpBuffer,
            UIntPtr nSize, IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern int CloseHandle(
            IntPtr hObject
        );

        [DllImport("kernel32")]
        public static extern bool IsWow64Process(IntPtr hProcess, out bool lpSystemInfo);
    }
}