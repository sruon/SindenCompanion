using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using System.Threading;
using Lightgun;
using SindenCompanionShared;
using Serilog;

namespace SindenHook
{
    public class EntryPoint
    {
        private ILogger _logger;
        private ServerInterface _client;
        private RecoilProfile _lastProfile;

        public List<SindenLightgun> Lightguns;
        EntryPoint() {
            _client = new ServerInterface(false, 5557, null, MessageHandler);
            _logger = SindenCompanionShared.Logger.CreateRemoteLogger(_client);
            _client.SetLogger(_logger);
        }

        public void Execute()
        {
            Form f1;
            try
            {
                f1 = Application.OpenForms["Form1"];
                _logger.Information("Found main app form");
            }
            catch (Exception ex)
            {
                Enveloppe ev = MessageBuilder.Build("exception", ex);
                _client.SendMessage(ev.AsMessage());
                throw ex;
            }
            FieldInfo type = typeof(Lightgun.Form1).GetField("listLightguns", BindingFlags.NonPublic | BindingFlags.Instance);
            Lightguns = (List<SindenLightgun>)type.GetValue(f1);
            _logger.Information("Found list of lightguns {@Count}", Lightguns.Count);

            while (true)
            {
                Thread.Sleep(5000);
            }
        }
        public void MessageHandler(List<string> messages)
        {
            foreach (string msg in messages)
            {
                if (msg != null && msg != string.Empty)
                {
                    Enveloppe e = MessageBuilder.FromMessage(msg);
                    Enveloppe ev;
                    switch (e.type)
                    {
                        case "ping":
                            ev = MessageBuilder.Build("pong", null);
                            _client.SendMessage(ev.AsMessage());
                            break;
                        case "report":
                            ev = MessageBuilder.Build("status", Lightguns);
                            _client.SendMessage(ev.AsMessage());
                            break;
                        case "recoil":
                            if (Lightguns.Count == 0 || _lastProfile == null)
                            {
                                ev = MessageBuilder.Build("recoilack", false);
                            }
                            else
                            {
                                foreach (SindenLightgun lightgun in Lightguns)
                                {
                                    if (!_lastProfile.Automatic)
                                    {
                                        lightgun.TestSingleShotRecoil();
                                    }
                                    else
                                    {
                                        lightgun.TestAutomaticRecoilStart();
                                        Thread.Sleep(500);
                                        lightgun.TestAutomaticRecoilStop();
                                    }
                                }
                                ev = MessageBuilder.Build("recoilack", true);
                            }
                            _client.SendMessage(ev.AsMessage());
                            break;
                        case "profile":
                            RecoilProfile rp = RecoilProfile.FromString(e.payload.ToString());
                            if (_lastProfile != null && _lastProfile.Name == rp.Name)
                            {
                                break;
                            }
                            if (Lightguns.Count == 0)
                            {
                                ev = MessageBuilder.Build("profileack", false);
                                _client.SendMessage(ev.AsMessage());
                            }
                            else
                            {
                                foreach (SindenLightgun lightgun in Lightguns)
                                {
                                    lightgun.UpdateRecoilFromProfile(rp);
                                }
                                _lastProfile = rp;
                                ev = MessageBuilder.Build("profileack", true);
                            }
                            _client.SendMessage(ev.AsMessage());
                            break;
                        default:
                            break;
                    }
                }
            
        }
    }
        static int Run(string test)
        {
            EntryPoint app = new EntryPoint();
            app.Execute();
            return 0;
        }
    }
}