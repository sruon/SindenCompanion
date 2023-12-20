using Lightgun;
using System.IO.Ports;
using System.Threading;
using SindenCompanionShared;

namespace SindenHook
{
    public static class SindenLightgunExtensions
    {
        public static bool UpdateRecoilFromProfile(this SindenLightgun l, RecoilProfile r)
        {
            var blockComPort = l.GetType().GetField("BlockComPort", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (blockComPort == null) return false;
            blockComPort.SetValue(l, true);
            var comPort = l.GetType().GetField("ComPort",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (comPort == null) return false;
            SerialPort comPortV = comPort.GetValue(l) as SerialPort;
            foreach (var payload in r.AsConfigurationPayload())
            {
                if (comPortV == null) return false;
                comPortV.Write(payload, 0, 7);
                Thread.Sleep(100);
            }
            blockComPort.SetValue(l, false);
            return true;
        }
    }
}
