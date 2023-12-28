using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using static System.String;
using static Memory.Imps;

namespace Memory
{
    /// <summary>
    /// Memory.dll class. Full documentation at https://github.com/erfg12/memory.dll/wiki
    /// </summary>
    public partial class Mem
    {
        public Proc MProc = new Proc();

        /// <summary>
        /// Open the PC game process with all security and access rights.
        /// </summary>
        /// <param name="pid">Use process name or process ID here.</param>
        /// <returns>Process opened successfully or failed.</returns>
        /// <param name="failReason">Show reason open process fails</param>
        public bool OpenProcess(int pid, out string failReason)
        {
            if (pid <= 0)
            {
                failReason = "OpenProcess given proc ID 0.";
                Debug.WriteLine("ERROR: OpenProcess given proc ID 0.");
                return false;
            }


            if (MProc.Process != null && MProc.Process.Id == pid)
            {
                failReason = "mProc.Process is null";
                return true;
            }

            try
            {
                MProc.Process = Process.GetProcessById(pid);

                if (MProc.Process != null && !MProc.Process.Responding)
                {
                    Debug.WriteLine("ERROR: OpenProcess: Process is not responding or null.");
                    failReason = "Process is not responding or null.";
                    return false;
                }

                MProc.Handle = Imps.OpenProcess(0x1F0FFF, true, pid);

                try
                {
                    Process.EnterDebugMode();
                }
                catch (Win32Exception)
                {
                    //Debug.WriteLine("WARNING: You are not running with raised privileges! Visit https://github.com/erfg12/memory.dll/wiki/Administrative-Privileges"); 
                }

                if (MProc.Handle == IntPtr.Zero)
                {
                    var eCode = Marshal.GetLastWin32Error();
                    Debug.WriteLine(
                        "ERROR: OpenProcess has failed opening a handle to the target process (GetLastWin32ErrorCode: " +
                        eCode + ")");
                    Process.LeaveDebugMode();
                    MProc = null;
                    failReason = "failed opening a handle to the target process(GetLastWin32ErrorCode: " + eCode + ")";
                    return false;
                }

                // Lets set the process to 64bit or not here (cuts down on api calls)
                MProc.Is64Bit = Environment.Is64BitOperatingSystem && IsWow64Process(MProc.Handle, out var retVal) &&
                                !retVal;

                MProc.MainModule = MProc.Process.MainModule;

                //GetModules();

                Debug.WriteLine("Process #" + MProc.Process + " is now open.");
                failReason = "";
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ERROR: OpenProcess has crashed. " + ex);
                failReason = "OpenProcess has crashed. " + ex;
                return false;
            }
        }


        /// <summary>
        /// Convert code from string to real address. If path is not blank, will pull from ini file.
        /// </summary>
        /// <param name="name">label in ini file or code</param>
        /// <param name="size">size of address (default is 8)</param>
        /// <returns></returns>
        public UIntPtr GetCode(string name, int size = 8)
        {
            var theCode = name;
            if (MProc == null)
                return UIntPtr.Zero;

            if (MProc.Is64Bit)
            {
                //Debug.WriteLine("Changing to 64bit code...");
                if (size == 8) size = 16; //change to 64bit
                return Get64BitCode(name, size); //jump over to 64bit code grab
            }

            if (IsNullOrEmpty(theCode))
                //Debug.WriteLine("ERROR: LoadCode returned blank. NAME:" + name + " PATH:" + path);
                return UIntPtr.Zero;


            // remove spaces
            if (theCode.Contains(" "))
                theCode = theCode.Replace(" ", Empty);

            if (!theCode.Contains("+") && !theCode.Contains(","))
                try
                {
                    return new UIntPtr(Convert.ToUInt32(theCode, 16));
                }
                catch
                {
                    Console.WriteLine("Error in GetCode(). Failed to read address " + theCode);
                    return UIntPtr.Zero;
                }

            var newOffsets = theCode;

            if (theCode.Contains("+"))
                newOffsets = theCode.Substring(theCode.IndexOf('+') + 1);

            var memoryAddress = new byte[size];

            if (newOffsets.Contains(','))
            {
                var offsetsList = new List<int>();

                var newerOffsets = newOffsets.Split(',');
                foreach (var oldOffsets in newerOffsets)
                {
                    var test = oldOffsets;
                    if (oldOffsets.Contains("0x")) test = oldOffsets.Replace("0x", "");
                    int preParse;
                    if (!oldOffsets.Contains("-"))
                    {
                        preParse = int.Parse(test, NumberStyles.AllowHexSpecifier);
                    }
                    else
                    {
                        test = test.Replace("-", "");
                        preParse = int.Parse(test, NumberStyles.AllowHexSpecifier);
                        preParse *= -1;
                    }

                    offsetsList.Add(preParse);
                }

                var offsets = offsetsList.ToArray();

                if (theCode.Contains("base") || theCode.Contains("main"))
                {
                    ReadProcessMemory(MProc.Handle, (UIntPtr)((int)MProc.MainModule.BaseAddress + offsets[0]),
                        memoryAddress, (UIntPtr)size, IntPtr.Zero);
                }
                else if (!theCode.Contains("base") && !theCode.Contains("main") && theCode.Contains("+"))
                {
                    var moduleName = theCode.Split('+');
                    var altModule = IntPtr.Zero;
                    if (!moduleName[0].ToLower().Contains(".dll") && !moduleName[0].ToLower().Contains(".exe") &&
                        !moduleName[0].ToLower().Contains(".bin"))
                    {
                        var theAddr = moduleName[0];
                        if (theAddr.Contains("0x")) theAddr = theAddr.Replace("0x", "");
                        altModule = (IntPtr)int.Parse(theAddr, NumberStyles.HexNumber);
                    }
                    else
                    {
                        try
                        {
                            altModule = GetModuleAddressByName(moduleName[0]);
                        }
                        catch
                        {
                            Debug.WriteLine("Module " + moduleName[0] + " was not found in module list!");
                            //Debug.WriteLine("Modules: " + string.Join(",", mProc.Modules));
                        }
                    }

                    ReadProcessMemory(MProc.Handle, (UIntPtr)((int)altModule + offsets[0]), memoryAddress,
                        (UIntPtr)size, IntPtr.Zero);
                }
                else
                {
                    ReadProcessMemory(MProc.Handle, (UIntPtr)offsets[0], memoryAddress, (UIntPtr)size, IntPtr.Zero);
                }

                var num1 = BitConverter.ToUInt32(memoryAddress, 0); //ToUInt64 causes arithmetic overflow.

                var base1 = (UIntPtr)0;

                for (var i = 1; i < offsets.Length; i++)
                {
                    base1 = new UIntPtr(Convert.ToUInt32(num1 + offsets[i]));
                    ReadProcessMemory(MProc.Handle, base1, memoryAddress, (UIntPtr)size, IntPtr.Zero);
                    num1 = BitConverter.ToUInt32(memoryAddress, 0); //ToUInt64 causes arithmetic overflow.
                }

                return base1;
            }
            else // no offsets
            {
                var trueCode = Convert.ToInt32(newOffsets, 16);
                var altModule = IntPtr.Zero;
                //Debug.WriteLine("newOffsets=" + newOffsets);
                if (theCode.ToLower().Contains("base") || theCode.ToLower().Contains("main"))
                {
                    altModule = MProc.MainModule.BaseAddress;
                }
                else if (!theCode.ToLower().Contains("base") && !theCode.ToLower().Contains("main") &&
                         theCode.Contains("+"))
                {
                    var moduleName = theCode.Split('+');
                    if (!moduleName[0].ToLower().Contains(".dll") && !moduleName[0].ToLower().Contains(".exe") &&
                        !moduleName[0].ToLower().Contains(".bin"))
                    {
                        var theAddr = moduleName[0];
                        if (theAddr.Contains("0x")) theAddr = theAddr.Replace("0x", "");
                        altModule = (IntPtr)int.Parse(theAddr, NumberStyles.HexNumber);
                    }
                    else
                    {
                        try
                        {
                            altModule = GetModuleAddressByName(moduleName[0]);
                        }
                        catch
                        {
                            Debug.WriteLine("Module " + moduleName[0] + " was not found in module list!");
                            //Debug.WriteLine("Modules: " + string.Join(",", mProc.Modules));
                        }
                    }
                }
                else
                {
                    altModule = GetModuleAddressByName(theCode.Split('+')[0]);
                }

                return (UIntPtr)((int)altModule + trueCode);
            }
        }

        /// <summary>
        /// Retrieve mProc.Process module baseaddress by name
        /// </summary>
        /// <param name="name">name of module</param>
        /// <returns></returns>
        public IntPtr GetModuleAddressByName(string name)
        {
            return MProc.Process.Modules.Cast<ProcessModule>()
                .SingleOrDefault(m =>
                {
                    if (m == null) throw new ArgumentNullException(nameof(m));
                    return string.Equals(m.ModuleName, name, StringComparison.OrdinalIgnoreCase);
                })
                .BaseAddress;
        }

        /// <summary>
        /// Convert code from string to real address. If path is not blank, will pull from ini file.
        /// </summary>
        /// <param name="name">label in ini file OR code</param>
        /// <param name="size">size of address (default is 16)</param>
        /// <returns></returns>
        public UIntPtr Get64BitCode(string name, int size = 16)
        {
            var theCode = name;

            if (IsNullOrEmpty(theCode))
                return UIntPtr.Zero;

            // remove spaces
            if (theCode.Contains(" "))
                theCode.Replace(" ", Empty);

            var newOffsets = theCode;
            if (theCode.Contains("+"))
                newOffsets = theCode.Substring(theCode.IndexOf('+') + 1);

            var memoryAddress = new byte[size];

            if (!theCode.Contains("+") && !theCode.Contains(","))
                try
                {
                    return new UIntPtr(Convert.ToUInt64(theCode, 16));
                }
                catch
                {
                    Console.WriteLine("Error in GetCode(). Failed to read address " + theCode);
                    return UIntPtr.Zero;
                }

            if (newOffsets.Contains(','))
            {
                var offsetsList = new List<long>();

                var newerOffsets = newOffsets.Split(',');
                foreach (var oldOffsets in newerOffsets)
                {
                    var test = oldOffsets;
                    if (oldOffsets.Contains("0x")) test = oldOffsets.Replace("0x", "");
                    long preParse = 0;
                    if (!oldOffsets.Contains("-"))
                    {
                        preParse = long.Parse(test, NumberStyles.AllowHexSpecifier);
                    }
                    else
                    {
                        test = test.Replace("-", "");
                        preParse = long.Parse(test, NumberStyles.AllowHexSpecifier);
                        preParse = preParse * -1;
                    }

                    offsetsList.Add(preParse);
                }

                var offsets = offsetsList.ToArray();

                if (theCode.Contains("base") || theCode.Contains("main"))
                {
                    ReadProcessMemory(MProc.Handle, (UIntPtr)((long)MProc.MainModule.BaseAddress + offsets[0]),
                        memoryAddress, (UIntPtr)size, IntPtr.Zero);
                }
                else if (!theCode.Contains("base") && !theCode.Contains("main") && theCode.Contains("+"))
                {
                    var moduleName = theCode.Split('+');
                    var altModule = IntPtr.Zero;
                    if (!moduleName[0].ToLower().Contains(".dll") && !moduleName[0].ToLower().Contains(".exe") &&
                        !moduleName[0].ToLower().Contains(".bin"))
                        altModule = (IntPtr)long.Parse(moduleName[0], NumberStyles.HexNumber);
                    else
                        try
                        {
                            altModule = GetModuleAddressByName(moduleName[0]);
                        }
                        catch
                        {
                            Debug.WriteLine("Module " + moduleName[0] + " was not found in module list!");
                            //Debug.WriteLine("Modules: " + string.Join(",", mProc.Modules));
                        }

                    ReadProcessMemory(MProc.Handle, (UIntPtr)((long)altModule + offsets[0]), memoryAddress,
                        (UIntPtr)size, IntPtr.Zero);
                }
                else // no offsets
                {
                    ReadProcessMemory(MProc.Handle, (UIntPtr)offsets[0], memoryAddress, (UIntPtr)size, IntPtr.Zero);
                }

                var num1 = BitConverter.ToInt64(memoryAddress, 0);

                var base1 = (UIntPtr)0;

                for (var i = 1; i < offsets.Length; i++)
                {
                    base1 = new UIntPtr(Convert.ToUInt64(num1 + offsets[i]));
                    ReadProcessMemory(MProc.Handle, base1, memoryAddress, (UIntPtr)size, IntPtr.Zero);
                    num1 = BitConverter.ToInt64(memoryAddress, 0);
                }

                return base1;
            }
            else
            {
                var trueCode = Convert.ToInt64(newOffsets, 16);
                var altModule = IntPtr.Zero;
                if (theCode.Contains("base") || theCode.Contains("main"))
                {
                    altModule = MProc.MainModule.BaseAddress;
                }
                else if (!theCode.Contains("base") && !theCode.Contains("main") && theCode.Contains("+"))
                {
                    var moduleName = theCode.Split('+');
                    if (!moduleName[0].ToLower().Contains(".dll") && !moduleName[0].ToLower().Contains(".exe") &&
                        !moduleName[0].ToLower().Contains(".bin"))
                    {
                        var theAddr = moduleName[0];
                        if (theAddr.Contains("0x")) theAddr = theAddr.Replace("0x", "");
                        altModule = (IntPtr)long.Parse(theAddr, NumberStyles.HexNumber);
                    }
                    else
                    {
                        try
                        {
                            altModule = GetModuleAddressByName(moduleName[0]);
                        }
                        catch
                        {
                            Debug.WriteLine("Module " + moduleName[0] + " was not found in module list!");
                            //Debug.WriteLine("Modules: " + string.Join(",", mProc.Modules));
                        }
                    }
                }
                else
                {
                    altModule = GetModuleAddressByName(theCode.Split('+')[0]);
                }

                return (UIntPtr)((long)altModule + trueCode);
            }
        }

        /// <summary>
        /// Close the process when finished.
        /// </summary>
        public void CloseProcess()
        {
            CloseHandle(MProc.Handle);
            MProc = null;
        }
    }
}