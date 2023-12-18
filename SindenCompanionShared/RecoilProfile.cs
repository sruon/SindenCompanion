using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SindenCompanionShared
{
    public class RecoilProfile
    {
        public string Name { get; set; }
        public bool Automatic { get; set; }
        public byte PulseLength { get; set; }
        public byte DelayBetweenPulses { get; set; }
        public byte DelayAfterFirstPulse { get; set; }
        public bool Offscreen { get; set; }

        public bool PumpOn {  get; set; }
        public bool PumpOff {  get; set; }

        public bool RecoilFrontLeft { get; set; }
        public bool RecoilBackLeft { get; set; }
        public bool RecoilFrontRight { get; set; }
        public bool RecoilBackRight { get; set; }

        public byte Strength { get; set; }

        public static RecoilProfile FromString(string s)
        {
            return JsonConvert.DeserializeObject<RecoilProfile>(s);
        }
        public List<byte[]> AsConfigurationPayload()
        {
            List<byte[]> ret = new List<byte[]>() {
                new byte[] { 170, 162, PulseLength, DelayAfterFirstPulse, PulseLength, DelayBetweenPulses, 187 },
                new byte[] {170, 161, 1, 0, 0, 0, 187},
                new byte[] {170, 167, Strength, 0, 0, 0, 187},
                new byte[] { 170, 163, Convert.ToByte(Automatic), 0, 0, 0, 187 },
                new byte[] {170, 164, 1, Convert.ToByte(Offscreen), Convert.ToByte(PumpOn), Convert.ToByte(PumpOff), 187 },
                new byte[] {170, 165, Convert.ToByte(RecoilFrontLeft), Convert.ToByte(RecoilBackLeft), Convert.ToByte(RecoilFrontRight), Convert.ToByte(RecoilBackRight), 187},
                new byte[] {170, 171, 4, 4, 4, 0, 187},
                //new byte[] { 170, 172, Strength, 0, 0, 0, 187} not sure what this does
            };

            return ret;
        }
    }
}
