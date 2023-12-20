using System;
using Newtonsoft.Json;

namespace SindenCompanionShared
{
    public class MessageBuilder
    {
        public static Envelope Build(string type, object payload)
        {
            var e = new Envelope(type, payload);
            return e;
        }

        public static Envelope FromMessage(string message)
        {
            if (message == null) return null;
            return JsonConvert.DeserializeObject<Envelope>(message);
        }
    }

    public class Envelope
    {
        private readonly DateTime _tstamp;
        public readonly object Payload;
        public readonly string Type;

        public Envelope(string type, object payload)
        {
            _tstamp = DateTime.Now;
            Payload = payload;
            Type = type;
        }

        public string AsMessage()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}