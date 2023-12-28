using System;
using System.Text;
using static Memory.Imps;

namespace Memory
{
    public partial class Mem
    {
        /// <summary>
        /// Reads up to `length ` bytes from an address.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="length">The maximum bytes to read.</param>
        /// <returns>The bytes read or null</returns>
        public byte[] ReadBytes(string code, long length)
        {
            var memory = new byte[length];
            var theCode = GetCode(code);
            if (theCode == UIntPtr.Zero || theCode.ToUInt64() < 0x10000)
                return null;

            if (!ReadProcessMemory(MProc.Handle, theCode, memory, (UIntPtr)length, IntPtr.Zero))
                return null;

            return memory;
        }

        /// <summary>
        /// Read a float value from an address.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="round">Round the value to 2 decimal places</param>
        /// <returns></returns>
        public float ReadFloat(string code, bool round = true)
        {
            var memory = new byte[4];

            var theCode = GetCode(code);
            if (theCode == UIntPtr.Zero || theCode.ToUInt64() < 0x10000)
                return 0;

            try
            {
                if (ReadProcessMemory(MProc.Handle, theCode, memory, (UIntPtr)4, IntPtr.Zero))
                {
                    var address = BitConverter.ToSingle(memory, 0);
                    var returnValue = (float)address;
                    if (round)
                        returnValue = (float)Math.Round(address, 2);
                    return returnValue;
                }
                else
                {
                    return 0;
                }
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Read a string value from an address.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="length">length of bytes to read (OPTIONAL)</param>
        /// <param name="zeroTerminated">terminate string at null char</param>
        /// <param name="stringEncoding">System.Text.Encoding.UTF8 (DEFAULT). Other options: ascii, unicode, utf32, utf7</param>
        /// <returns></returns>
        public string ReadString(string code, int length = 32, bool zeroTerminated = true,
            Encoding stringEncoding = null)
        {
            if (stringEncoding == null)
                stringEncoding = Encoding.UTF8;

            var memoryNormal = new byte[length];
            var theCode = GetCode(code);
            if (theCode == UIntPtr.Zero || theCode.ToUInt64() < 0x10000)
                return "";

            if (ReadProcessMemory(MProc.Handle, theCode, memoryNormal, (UIntPtr)length, IntPtr.Zero))
                return zeroTerminated
                    ? stringEncoding.GetString(memoryNormal).Split('\0')[0]
                    : stringEncoding.GetString(memoryNormal);
            else
                return "";
        }

        /// <summary>
        /// Read a double value
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="round">Round the value to 2 decimal places</param>
        /// <returns></returns>
        public double ReadDouble(string code, bool round = true)
        {
            var memory = new byte[8];

            var theCode = GetCode(code);
            if (theCode == UIntPtr.Zero || theCode.ToUInt64() < 0x10000)
                return 0;

            try
            {
                if (ReadProcessMemory(MProc.Handle, theCode, memory, (UIntPtr)8, IntPtr.Zero))
                {
                    var address = BitConverter.ToDouble(memory, 0);
                    var returnValue = (double)address;
                    if (round)
                        returnValue = (double)Math.Round(address, 2);
                    return returnValue;
                }
                else
                {
                    return 0;
                }
            }
            catch
            {
                return 0;
            }
        }


        /// <summary>
        /// Read an integer from an address.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <returns></returns>
        public int ReadInt(string code)
        {
            var memory = new byte[4];
            var theCode = GetCode(code);
            if (theCode == UIntPtr.Zero || theCode.ToUInt64() < 0x10000)
                return 0;

            if (ReadProcessMemory(MProc.Handle, theCode, memory, (UIntPtr)4, IntPtr.Zero))
                return BitConverter.ToInt32(memory, 0);
            else
                return 0;
        }

        /// <summary>
        /// Read a long value from an address.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <returns></returns>
        public long ReadLong(string code)
        {
            var memory = new byte[16];
            var theCode = GetCode(code);

            if (theCode == UIntPtr.Zero || theCode.ToUInt64() < 0x10000)
                return 0;

            if (ReadProcessMemory(MProc.Handle, theCode, memory, (UIntPtr)8, IntPtr.Zero))
                return BitConverter.ToInt64(memory, 0);
            else
                return 0;
        }

        /// <summary>
        /// Read a UInt value from address.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <returns></returns>
        public uint ReadUInt(string code)
        {
            var memory = new byte[4];
            var theCode = GetCode(code);
            if (theCode == UIntPtr.Zero || theCode.ToUInt64() < 0x10000)
                return 0;

            if (ReadProcessMemory(MProc.Handle, theCode, memory, (UIntPtr)4, IntPtr.Zero))
                return BitConverter.ToUInt32(memory, 0);
            else
                return 0;
        }


        /// <summary>
        /// Read a 2 byte value from an address. Returns an integer.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <returns></returns>
        public int Read2Byte(string code)
        {
            var memoryTiny = new byte[4];

            var theCode = GetCode(code);
            if (theCode == UIntPtr.Zero || theCode.ToUInt64() < 0x10000)
                return 0;

            if (ReadProcessMemory(MProc.Handle, theCode, memoryTiny, (UIntPtr)2, IntPtr.Zero))
                return BitConverter.ToInt32(memoryTiny, 0);
            else
                return 0;
        }

        /// <summary>
        /// Read 1 byte from address.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <returns></returns>
        public int ReadByte(string code)
        {
            var memoryTiny = new byte[1];

            var theCode = GetCode(code);
            if (theCode == UIntPtr.Zero || theCode.ToUInt64() < 0x10000)
                return 0;

            if (ReadProcessMemory(MProc.Handle, theCode, memoryTiny, (UIntPtr)1, IntPtr.Zero))
                return memoryTiny[0];

            return 0;
        }


        public T ReadMemory<T>(string address)
        {
            object readOutput = null;

            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.String:
                    readOutput = ReadString(address);
                    break;
                case TypeCode.Int32:
                    readOutput = ReadInt(address);
                    break;
                case TypeCode.Int64:
                    readOutput = ReadLong(address);
                    break;
                case TypeCode.Byte:
                    readOutput = ReadByte(address);
                    break;
                case TypeCode.Double:
                    readOutput = ReadDouble(address);
                    break;
                case TypeCode.Decimal:
                    readOutput = ReadFloat(address);
                    break;
                case TypeCode.UInt32:
                    readOutput = ReadUInt(address);
                    break;
                default:
                    break;
            }

            if (readOutput != null)
                return (T)Convert.ChangeType(readOutput, typeof(T));
            else
                return default;
        }
    }
}