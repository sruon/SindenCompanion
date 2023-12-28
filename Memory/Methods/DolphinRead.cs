using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;

namespace Memory
{
    public partial class Mem
    {
        private DolphinEmulationInformation _dolphin;

        /// <summary>
        /// Read 1 byte from address.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <returns></returns>
        public int ReadDolphinByte(string code)
        {
            if (_dolphin == null) _dolphin = new DolphinEmulationInformation(MProc.Process);

            // This re-checks memory pages on every call but not sure how to detect if ROM has changed
            if (!_dolphin.Load())
            {
                // Emulation may not yet have started
                _dolphin = null;
                return 0;
            }

            return _dolphin.ReadByte(code);
        }


        private class DolphinEmulationInformation
        {
            private const uint Mem1Start = 0x80000000;
            private const uint Mem2Start = 0x90000000;
            private readonly Process _mProc;
            private ulong _emuRamAddressStart;
            private bool _mem1Found;
            private ulong _mem2AddressStart;
            private bool _mem2Present;

            public DolphinEmulationInformation(Process proc)
            {
                _mProc = proc;
            }

            private ulong DolphinToPhysical(ulong addr)
            {
                if (addr >= Mem2Start)
                    return _mem2AddressStart + addr - Mem2Start;
                return _emuRamAddressStart + addr - Mem1Start;
            }

            /// <summary>
            /// Convert code from string to real address.
            /// </summary>
            /// <param name="name">label in ini file OR code</param>
            /// <returns></returns>
            private UIntPtr GetDolphinCode(string name)
            {
                // Remove spaces and split the input string by ","
                var parts = name.Replace(" ", "").Split(',');

                // If there are no parts, return UIntPtr.Zero
                if (parts.Length == 0)
                    return UIntPtr.Zero;

                // Initialize memoryAddress array
                var memoryAddress = new byte[4];
                ulong baseAddress = 0;

                // Loop through the remaining parts and calculate the final baseAddress
                for (var i = 0; i < parts.Length; i++)
                {
                    if (!ulong.TryParse(parts[i].Replace("0x", ""), NumberStyles.HexNumber, null, out var offset))
                        return UIntPtr.Zero;

                    // Convert offset to UIntPtr and add it to the baseAddress
                    baseAddress += offset;
                    if (i == parts.Length - 1) return (UIntPtr)DolphinToPhysical(baseAddress);
                    Imps.ReadProcessMemory(_mProc.Handle, (UIntPtr)DolphinToPhysical(baseAddress), memoryAddress,
                        (UIntPtr)4, IntPtr.Zero);
                    baseAddress = BinaryPrimitives.ReadUInt32BigEndian(memoryAddress);
                }

                return (UIntPtr)DolphinToPhysical(baseAddress);
            }

            public int ReadByte(string code)
            {
                var memoryTiny = new byte[1];

                var theCode = GetDolphinCode(code);
                if (theCode == UIntPtr.Zero || theCode.ToUInt64() < 0x10000)
                    return 0;

                return Imps.ReadProcessMemory(_mProc.Handle, theCode, memoryTiny, (UIntPtr)1, IntPtr.Zero)
                    ? memoryTiny[0]
                    : 0;
            }

            public unsafe bool Load()
            {
                Imps.PsapiWorkingSetExInformation[] setInformation;
                for (ulong p = 0;
                     Imps.VirtualQueryEx(_mProc.Handle, (IntPtr)p, out var info,
                         (uint)sizeof(Imps.MemoryBasicInformation)) ==
                     sizeof(Imps.MemoryBasicInformation);
                     p += (ulong)info.RegionSize)
                    // Check region size so that we know it's MEM2
                    if (!_mem2Present && info.RegionSize == (IntPtr)0x4000000)
                    {
                        var regionBaseAddress = (ulong)info.BaseAddress;
                        if (_mem1Found && regionBaseAddress > _emuRamAddressStart + 0x10000000)
                            // In some cases MEM2 could actually be before MEM1. Once we find MEM1, ignore regions of
                            // this size that are too far away. There apparently are other non-MEM2 regions of 64 MiB.
                            break;
                        // View the comment for MEM1.
                        setInformation = new Imps.PsapiWorkingSetExInformation[1];
                        setInformation[0].VirtualAddress = info.BaseAddress;
                        if (!Imps.QueryWorkingSetEx(_mProc.Handle, setInformation,
                                sizeof(Imps.PsapiWorkingSetExInformation))) continue;
                        if (!setInformation[0].VirtualAttributes.Valid) continue;
                        _mem2AddressStart = regionBaseAddress;
                        _mem2Present = true;
                    }
                    else if (info.RegionSize == (IntPtr)0x2000000 && info.LType == Imps.PageType.Mapped)
                    {
                        // Here, it's likely the right page, but it can happen that multiple pages with these criteria
                        // exists and have nothing to do with the emulated memory. Only the right page has valid
                        // working set information so an additional check is required that it is backed by physical
                        // memory.
                        setInformation = new Imps.PsapiWorkingSetExInformation[1];
                        setInformation[0].VirtualAddress = info.BaseAddress;
                        if (!Imps.QueryWorkingSetEx(_mProc.Handle, setInformation,
                                sizeof(Imps.PsapiWorkingSetExInformation))) continue;
                        if (!setInformation[0].VirtualAttributes.Valid) continue;
                        if (!_mem1Found)
                        {
                            _emuRamAddressStart = (ulong)info.BaseAddress;
                            _mem1Found = true;
                        }
                        else
                        {
                            var aramCandidate = (ulong)info.BaseAddress;
                            if (aramCandidate != _emuRamAddressStart + 0x2000000) continue;
                            _emuAramAddressStart = aramCandidate;
                            _aramAccessible = true;
                        }
                    }


                if (!_mem2Present) return _emuRamAddressStart != 0;
                _emuAramAddressStart = 0;
                _aramAccessible = false;

                // Here, Dolphin is running, but the emulation hasn't started
                return _emuRamAddressStart != 0;
            }
#pragma warning disable CS0414 // Field is assigned but its value is never used
            private bool _aramAccessible;
            private ulong _emuAramAddressStart;
#pragma warning restore CS0414 // Field is assigned but its value is never used
        }
    }
}