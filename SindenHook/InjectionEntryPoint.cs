using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Lightgun;
using Newtonsoft.Json;
using Serilog;
using SindenCompanionShared;

namespace SindenHook
{
    public class EntryPoint
    {
        private readonly ServerInterface _client;
        private readonly ILogger _logger;
        private RecoilProfile _lastProfile;

        public List<SindenLightgun> Lightguns;

        private EntryPoint(InjectionArguments injectionArguments)
        {
            _client = new ServerInterface(false, injectionArguments.CommunicationPort, null, MessageHandler);
            _logger = Logger.CreateRemoteLogger(_client);
            _client.SetLogger(_logger);
        }

        public void Execute()
        {
            Form f1 = null;
            while (f1 == null)
            {
                Thread.Sleep(200);
                try
                {
                    f1 = Application.OpenForms["Form1"];
                }
                catch (Exception ex)
                {
                    var ev = MessageBuilder.Build("exception", ex);
                    _client.SendMessage(ev.AsMessage());
                }
            }

            _logger.Information("Found main app form");


            var foundGuns = false;

            while (!foundGuns)
            {
                Thread.Sleep(200);

                try
                {
                    var type =
                        typeof(Form1).GetField("listLightguns", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (type != null)
                    {
                        Lightguns = (List<SindenLightgun>)type.GetValue(f1);
                        _logger.Information("Found list of lightguns {@Count}", Lightguns.Count);
                    }
                    else
                    {
                        continue;
                    }

                    foundGuns = true;
                }
                catch (Exception ex)
                {
                    var ev = MessageBuilder.Build("exception", ex);
                    _client.SendMessage(ev.AsMessage());
                }
            }

            while (true) Thread.Sleep(5000);
        }

        public void MessageHandler(List<string> messages)
        {
            foreach (var msg in messages)
                if (!string.IsNullOrEmpty(msg))
                {
                    var e = MessageBuilder.FromMessage(msg);
                    Envelope ev;
                    switch (e.Type)
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
                            if (Lightguns == null || Lightguns.Count == 0 || _lastProfile == null)
                            {
                                ev = MessageBuilder.Build("recoilack", false);
                                _client.SendMessage(ev.AsMessage());
                                break;
                            }

                            foreach (var lightgun in Lightguns)
                                if (!_lastProfile.Automatic)
                                {
                                    lightgun.TestSingleShotRecoil();
                                }
                                else
                                {
                                    lightgun.TestAutomaticRecoilStart();
                                    Thread.Sleep(200);
                                    lightgun.TestAutomaticRecoilStop();
                                }

                            ev = MessageBuilder.Build("recoilack", true);


                            _client.SendMessage(ev.AsMessage());
                            break;
                        case "profile":
                            if (Lightguns == null || Lightguns.Count == 0)
                            {
                                ev = MessageBuilder.Build("profileack", false);
                                _client.SendMessage(ev.AsMessage());
                                break;
                            }

                            var rp = RecoilProfile.FromString(e.Payload.ToString());
                            if (_lastProfile != null && _lastProfile.Name == rp.Name) break;

                            var success = false;
                            foreach (var lightgun in Lightguns)
                            {
                                success = lightgun.UpdateRecoilFromProfile(rp);
                                if (success) continue;
                                _logger.Error($"Failed to apply profile to {lightgun.lightgunOwner.ToString()}");
                                break;
                            }

                            if (success) _lastProfile = rp;
                            ev = MessageBuilder.Build("profileack", success);

                            _client.SendMessage(ev.AsMessage());
                            break;
                    }
                }
        }


        private static int Run(string args)
        {
            var injectionArguments = JsonConvert.DeserializeObject<InjectionArguments>(args);
            var app = new EntryPoint(injectionArguments);
            app.Execute();
            return 0;
        }
    }
}