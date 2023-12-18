using Newtonsoft.Json;
using System;


namespace SindenCompanionShared
{
    public class MessageBuilder
    {
        public MessageBuilder() {         
        }

        public static Enveloppe Build(string type, object payload)
        {
            Enveloppe e = new Enveloppe(type, payload);
            return e;
        }

        public static Enveloppe FromMessage(string message)
        {
            if (message == null)
            {
                return null;
            }
            return JsonConvert.DeserializeObject<Enveloppe>(message);
        }
    }
    public class Enveloppe
    {
        public DateTime tstamp;
        public object payload;
        public string type;
        public Enveloppe(string _type, object _payload) {
            tstamp = DateTime.Now;
            payload = _payload;
            type = _type;
        }

        public string AsMessage()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
