using System;
using System.Collections.Generic;
using System.Linq;
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
        private bool _isReady;
        private List<RecoilProfile> _lastProfile = new List<RecoilProfile> { null, null };

        private List<SindenLightgun> _lightguns;

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
                    _client.BuildAndSendMessage("exception", ex);
                }
            }

            _logger.Information("Found main app form");


            while (!_isReady)
            {
                Thread.Sleep(2000);

                try
                {
                    var type =
                        typeof(Form1).GetField("listLightguns", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (type == null) continue;

                    _lightguns = (List<SindenLightgun>)type.GetValue(f1);
                    if (_lightguns.Count == 0)
                    {
                        _logger.Information("Waiting for driver to finish initializing guns");
                        continue;
                    }

                    _logger.Information("Found list with {@Count} guns", _lightguns.Count);
                    _isReady = true;
                    _client.BuildAndSendMessage("ready", _lightguns);
                }
                catch (Exception ex)
                {
                    _client.BuildAndSendMessage("exception", ex);
                }
            }

            while (true) Thread.Sleep(5000);
        }

        public void MessageHandler(List<string> messages)
        {
            foreach (var msg in messages)
                if (!string.IsNullOrEmpty(msg))
                {
                    var e = MessageBuilder.FromString(msg);
                    Envelope ev;
                    switch (e.Type)
                    {
                        case "serverready":
                            if (_lightguns != null && _lightguns.Count > 0)
                                _client.BuildAndSendMessage("ready", _lightguns);
                            break;
                        case "ping":
                            _client.BuildAndSendMessage("pong", null);
                            break;
                        case "report":
                            _client.BuildAndSendMessage("status", _lightguns);
                            break;
                        case "recoil":
                            var pIndex = Convert.ToInt32(e.Payload);
                            if (_lightguns == null || _lightguns.Count == 0 ||
                                _lastProfile.ElementAtOrDefault(Math.Max(pIndex, 0)) == null)
                            {
                                _client.BuildAndSendMessage("recoilack", new RecoilResponse
                                {
                                    Reason = "Sinden drivers not yet initialized or no lightguns connected.",
                                    Success = false
                                });
                                break;
                            }

                            if (pIndex == -1)
                            {
                                foreach (var lightgun in _lightguns)
                                    if (!_lastProfile.ElementAtOrDefault(0).Automatic)
                                    {
                                        lightgun.TestSingleShotRecoil();
                                    }
                                    else
                                    {
                                        lightgun.TestAutomaticRecoilStart();
                                        Thread.Sleep(200);
                                        lightgun.TestAutomaticRecoilStop();
                                    }
                            }
                            else
                            {
                                if (_lightguns.ElementAtOrDefault(pIndex) != null)
                                {
                                    if (!_lastProfile[pIndex].Automatic)
                                    {
                                        _lightguns[pIndex].TestSingleShotRecoil();
                                    }
                                    else
                                    {
                                        _lightguns[pIndex].TestAutomaticRecoilStart();
                                        Thread.Sleep(200);
                                        _lightguns[pIndex].TestAutomaticRecoilStop();
                                    }
                                }
                                else
                                {
                                    _client.BuildAndSendMessage("recoilack", new RecoilResponse
                                    {
                                        Reason = "No lightgun at requested index.",
                                        Success = false
                                    });
                                    break;
                                }
                            }

                            _client.BuildAndSendMessage("recoilack", new RecoilResponse
                            {
                                Success = true
                            });
                            break;
                        case "profile":
                            if (_lightguns == null || _lightguns.Count == 0)
                            {
                                _client.BuildAndSendMessage("profileack",
                                    new RecoilProfileResponse
                                    {
                                        Success = false,
                                        Reason = "Sinden drivers not yet initialized or no lightguns connected."
                                    });
                                break;
                            }

                            var rpw = RecoilProfileWrapper.FromString(e.Payload.ToString());
                            var rp = rpw.RecoilProfile;
                            if (_lastProfile.ElementAtOrDefault(Math.Max(rpw.Player, 0)) != null &&
                                _lastProfile.ElementAtOrDefault(Math.Max(rpw.Player, 0)).Name == rp.Name) break;

                            var response = new RecoilProfileResponse();
                            if (rpw.Player == -1)
                            {
                                foreach (var lightgun in _lightguns)
                                {
                                    response.Success = lightgun.UpdateRecoilFromProfile(rp);
                                    if (response.Success) continue;
                                    response.Reason = $"Failed to apply profile to {lightgun.lightgunOwner.ToString()}";
                                    break;
                                }
                            }
                            else
                            {
                                if (_lightguns.ElementAtOrDefault(rpw.Player) != null)
                                {
                                    response.Success = _lightguns[rpw.Player].UpdateRecoilFromProfile(rp);
                                    if (!response.Success)
                                        response.Reason =
                                            $"Failed to apply profile to {_lightguns[rpw.Player].lightgunOwner.ToString()}";
                                }
                                else
                                {
                                    response.Success = false;
                                    response.Reason = $"No lightgun at requested index.";
                                }
                            }

                            if (response.Success)
                            {
                                if (rpw.Player == -1)
                                    _lastProfile = new List<RecoilProfile> { rp, rp };
                                else
                                    _lastProfile[rpw.Player] = rp;
                            }

                            _client.BuildAndSendMessage("profileack", response);
                            break;
                    }
                }
        }


#pragma warning disable IDE0051
        // ReSharper disable once UnusedMember.Local
        private static int Run(string args)
#pragma warning restore IDE0051
        {
            var injectionArguments = JsonConvert.DeserializeObject<InjectionArguments>(args);
            Thread.CurrentThread.IsBackground = true;
            var app = new EntryPoint(injectionArguments);
            app.Execute();
            return 0;
        }
    }
}