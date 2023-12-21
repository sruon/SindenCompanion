using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Memory;
using Serilog;
using SindenCompanionShared;

namespace SindenCompanion
{
    public class App : IDisposable
    {
        private readonly ILogger _logger;
        private readonly Dictionary<uint, Mem> _memReaders;
        private readonly ServerInterface _server;
        private Config _conf;
        private RecoilProfile _currentProfile;

        public App(ILogger logger)
        {
            _memReaders = new Dictionary<uint, Mem>();
            _conf = Config.GetInstance();
            _logger = logger;
            _server = new ServerInterface(true, _conf.Global.IpcPort, logger, MessageHandler);
        }

        public void Dispose()
        {
            _server.Dispose();
            foreach (var memlib in _memReaders.Values) memlib.CloseProcess();
        }

        public void ChangeProfile(RecoilProfile rp)
        {
            _server.SendMessage(MessageBuilder.Build("profile", rp).AsMessage());
            if (_conf.Global.RecoilOnSwitch) _server.SendMessage(MessageBuilder.Build("recoil", null).AsMessage());
            _currentProfile = rp;
        }

        private Mem GetMemoryReader(uint processId)
        {
            Mem memlib;
            if (_memReaders.TryGetValue(processId, out memlib)) return memlib;
            _memReaders[processId] = new Mem();
            var suc = _memReaders[processId].OpenProcess((int)processId, out var failReason);
            if (!suc)
            {
                _logger.Error("Failed to open process for memory reading: {@failReason}", failReason);
                throw new Exception("Failed to open process for memory reading");
            }
            else
            {
                _logger.Information("Successfully opened process for memory reading");
                memlib = _memReaders[processId];
            }

            return memlib;
        }

        public void WindowEventHandler(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild,
            uint dwEventThread, uint dwmsEventTime)
        {
            _logger.Information("Foreground window changed");
            _conf = Config.GetInstance();
            var fp = new ForegroundProcess();
            var matchedGp = _conf.GameProfiles.FirstOrDefault(x => x.Matches(fp));
            if (matchedGp != null)
            {
                RecoilProfile matchedRp = null;
                if (matchedGp.Memscan != null) // Continuous swap based on memory
                {
                    Mem memlib;

                    try
                    {
                        memlib = GetMemoryReader(fp.ProcessId);
                    }
                    catch
                    {
                        return;
                    }

                    _logger.Debug("Starting thread to watch memory for changes.");
                    new Thread(() =>
                    {
                        while (true)
                        {
                            var newFp = new ForegroundProcess();
                            if (newFp.ProcessId != fp.ProcessId)
                            {
                                _logger.Information("Detected window swap during memory scan, terminating thread.");
                                return;
                            }

                            dynamic value;
                            string profName = null;
                            switch (matchedGp.Memscan.Type)
                            {
                                case "byte":
                                    value = memlib.ReadByte(matchedGp.Memscan.Code);
                                    matchedGp.Memscan.Match.TryGetValue(value, out profName);
                                    break;
                                case "short": 
                                    value = memlib.Read2Byte(matchedGp.Memscan.Code);
                                    matchedGp.Memscan.Match.TryGetValue(value, out profName);
                                    break;
                                case "int":
                                    value = memlib.ReadInt(matchedGp.Memscan.Code);
                                    matchedGp.Memscan.Match.TryGetValue(value, out profName);
                                    break;
                                case "uint":
                                    value = memlib.ReadUInt(matchedGp.Memscan.Code);
                                    matchedGp.Memscan.Match.TryGetValue(value, out profName);
                                    break;
                                default:
                                    _logger.Error("Unsupported memory scan type: {@Type}", matchedGp.Memscan.Type);
                                    return;
                            }

                            if (!string.IsNullOrEmpty(profName))
                            {
                                matchedRp = _conf.RecoilProfiles.FirstOrDefault(p => p.Name == profName);
                                if (matchedRp == null)
                                {
                                    _logger.Error(
                                        "[{@Game}][MEM] {@Value} -> {@Profile} not found. Check your configuration.",
                                        matchedGp.Name, value, profName);
                                    continue;
                                }
                            }
                            else
                            {
                                _logger.Error("[{@Game}][MEM] {@Value} -> No profile found. Check your configuration.",
                                    matchedGp.Name, value);
                                continue;
                            }

                            if (matchedRp != _currentProfile)
                            {
                                _logger.Information("[{@Game}][MEM] {@Value} -> {@Profile}", matchedGp.Name, value,
                                    matchedRp.Name);
                                ChangeProfile(matchedRp);
                            }


                            Thread.Sleep(100);
                        }
                    }).Start();
                }
                else // Single profile swap
                {
                    matchedRp = _conf.RecoilProfiles.FirstOrDefault(p => p.Name == matchedGp.Profile);
                    if (matchedRp == null)
                    {
                        _logger.Error("Could not find any profile named {@Profile}. Check your configuration.",
                            matchedGp.Profile);
                        return;
                    }

                    if (matchedRp != _currentProfile)
                    {
                        _logger.Information("[{@Game}] {@Profile}", matchedGp.Name, matchedRp.Name);
                        ChangeProfile(matchedRp);
                    }
                }
            }
        }

        public void MessageHandler(List<string> messages)
        {
            foreach (var e in from msg in messages
                     where !string.IsNullOrEmpty(msg)
                     select MessageBuilder.FromMessage(msg))
                switch (e.Type)
                {
                    case "ready": 
                        _logger.Information("Client signaled it's ready.");
                        break;
                    case "recoilack":
                        if (!(bool)e.Payload)
                            _logger.Error($"Failed to recoil");
                        else
                            _logger.Information($"Recoil ACK - Success");
                        break;
                    case "profileack":
                        var suc = (bool)e.Payload;
                        if (!suc)
                        {
                            _logger.Error($"Failed to apply profile, Sinden may still be initializing - will retry");
                            _currentProfile = null;
                        }
                        else
                        {
                            _logger.Information("Successfully applied profile. {@Profile}", _currentProfile);
                        }

                        break;
                }
        }
    }

    internal class Program
    {
        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_SYSTEM_FOREGROUND = 3;
        private static WinEventDelegate _dele;

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [STAThread]
        private static void Main(string[] args)
        {
            var conf = Config.GetInstance();
            var mainForm = new AppForm(conf);
            var logger = Logger.CreateDesktopLogger(conf.Global.Debug, mainForm.WpfRichTextBox);
            var app = new App(logger);
            mainForm.SetCallback(app.ChangeProfile);
            SindenInjector injector = null;
            new Thread(() =>
            {
                injector = new SindenInjector(conf, logger);
                injector.Inject();
                while (true)
                {
                    if (!injector.IsAlive())
                    {
                        logger.Error("Lightgun.exe died, will attempt to restart. This will fail if path is not set.");
                        injector = new SindenInjector(conf, logger);
                        injector.Inject();
                    }

                    Thread.Sleep(1000);
                }
            }).Start();


            _dele = new WinEventDelegate(app.WindowEventHandler);
            SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _dele, 0, 0,
                WINEVENT_OUTOFCONTEXT);
            Application.ApplicationExit += (s, a) =>
            {
                if (injector != null) injector.Dispose();
                app.Dispose();
            };
            Application.EnableVisualStyles();
            Application.Run(mainForm);
        }

        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject,
            int idChild, uint dwEventThread, uint dwmsEventTime);
    }
}