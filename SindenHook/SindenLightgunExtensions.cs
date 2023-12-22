using System.IO.Ports;
using System.Reflection;
using System.Threading;
using Lightgun;
using SindenCompanionShared;

namespace SindenHook
{
    public static class SindenLightgunExtensions
    {
        public static bool SetBlockComPort(this SindenLightgun l, bool block)
        {
            var blockComPort = l.GetType().GetField("BlockComPort", BindingFlags.NonPublic | BindingFlags.Instance);
            if (blockComPort == null) return false;
            blockComPort.SetValue(l, block);
            return true;
        }

        public static SerialPort GetComPort(this SindenLightgun l)
        {
            var comPort = l.GetType().GetField("ComPort", BindingFlags.NonPublic | BindingFlags.Instance);
            if (comPort == null) return null;
            return comPort.GetValue(l) as SerialPort;
        }

        public static bool UpdateRecoilFromProfile(this SindenLightgun l, RecoilProfile r)
        {
            SetBlockComPort(l, true);
            var comPort = GetComPort(l);
            foreach (var payload in r.AsConfigurationPayload())
            {
                if (comPort == null) return false;
                comPort.Write(payload, 0, 7);
                Thread.Sleep(100);
            }

            SetBlockComPort(l, false);
            return true;
        }

        public static bool ChangeInputType(this SindenLightgun l, bool joystickMode)
        {
            var payload = new byte[]
            {
                170, 182, joystickMode ? (byte)1 : (byte)0, 0, 0, 0, 187
            };
            SetBlockComPort(l, true);
            var comPort = GetComPort(l);
            if (comPort == null) return false;
            comPort.Write(payload, 0, 7);
            SetBlockComPort(l, false);
            return true;
        }

        public static bool ChangeOffscreenReload(this SindenLightgun l, bool offscreen)
        {
            var payload = new byte[]
            {
                170, offscreen ? (byte)54 : (byte)55, 0, 0, 0, 0, 187
            };

            SetBlockComPort(l, true);
            var comPort = GetComPort(l);
            if (comPort == null) return false;
            comPort.Write(payload, 0, 7);
            SetBlockComPort(l, false);
            return true;
        }
    }
}