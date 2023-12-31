﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SindenCompanionShared
{
    public class Response<T>
    {
        public bool Success { get; set; }
        public string Reason { get; set; }

        public static Response<T> FromString(string s)
        {
            return JsonConvert.DeserializeObject<Response<T>>(s);
        }
    }

    public class RecoilResponse : Response<RecoilResponse>
    {
    }

    public class RecoilProfileResponse : Response<RecoilResponse>
    {
        public int PlayerIndex { get; set; }
    }

    public class RecoilProfileWrapper
    {
        public RecoilProfile RecoilProfile { get; set; }

        // -1: all
        // 0: player 1
        // 1: player 2
        public int Player { get; set; }

        public static RecoilProfileWrapper FromString(string s)
        {
            return JsonConvert.DeserializeObject<RecoilProfileWrapper>(s);
        }
    }

    public class RecoilProfile
    {
        public string Name { get; set; }
        public bool Automatic { get; set; }
        public int PulseLength { get; set; }
        public int DelayBetweenPulses { get; set; }
        public int DelayAfterFirstPulse { get; set; }
        public bool Offscreen { get; set; }

        public bool PumpOn { get; set; }
        public bool PumpOff { get; set; }

        public bool RecoilFrontLeft { get; set; }
        public bool RecoilBackLeft { get; set; }
        public bool RecoilFrontRight { get; set; }
        public bool RecoilBackRight { get; set; }

        public int Strength { get; set; }


        public List<byte[]> AsConfigurationPayload()
        {
            var ret = new List<byte[]>()
            {
                new byte[]
                {
                    170, 162, (byte)PulseLength, (byte)DelayAfterFirstPulse, (byte)PulseLength,
                    (byte)DelayBetweenPulses, 187
                },
                new byte[] { 170, 161, 1, 0, 0, 0, 187 },
                new byte[] { 170, 167, (byte)Strength, 0, 0, 0, 187 },
                new byte[] { 170, 163, Convert.ToByte(Automatic), 0, 0, 0, 187 },
                new byte[]
                {
                    170, 164, 1, Convert.ToByte(Offscreen), Convert.ToByte(PumpOn), Convert.ToByte(PumpOff), 187
                },
                new byte[]
                {
                    170, 165, Convert.ToByte(RecoilFrontLeft), Convert.ToByte(RecoilBackLeft),
                    Convert.ToByte(RecoilFrontRight), Convert.ToByte(RecoilBackRight), 187
                },
                new byte[] { 170, 171, 4, 4, 4, 0, 187 }
                //new byte[] { 170, 172, Strength, 0, 0, 0, 187} not sure what this does
            };

            return ret;
        }
    }
}