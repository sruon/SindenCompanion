using Lightgun;
using System.IO.Ports;
using System.Threading;
using SindenCompanionShared;

namespace SindenHook
{
    public static class SindenLightgunExtensions
    {
        public static void UpdateRecoilFromProfile(this SindenLightgun l, RecoilProfile r)
        {
            var _blockComPort = l.GetType().GetField("BlockComPort", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            _blockComPort.SetValue(l, true);
            var _ComPort = l.GetType().GetField("ComPort", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var ComPort = _ComPort.GetValue(l) as SerialPort;
            foreach (byte[] payload in r.AsConfigurationPayload())
            {
                ComPort.Write(payload, 0, 7);
                Thread.Sleep(50);
            }

            _blockComPort.SetValue(l, false);
        }
    }
}
