using System;
using SindenCompanionShared;
using System.Linq;
using System.Collections.Generic;
using System.Dynamic;
using Serilog;
using Memory;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;

namespace SindenCompanion
{
    public class App : IDisposable
    {
        private Dictionary<uint, Mem> _memReaders;
        private Config _conf;
        private ILogger _logger;
        private ServerInterface _server;
        private RecoilProfile _currentProfile = null;

        public App(ILogger logger) { 
            _memReaders = new Dictionary<uint, Mem>();
            _conf = Config.GetInstance();
            _logger = logger;
            _server = new ServerInterface(true, _conf.Global.IpcPort, logger, MessageHandler);
        }

        public void Dispose()
        {
            _server.Dispose();
            foreach (var memlib in _memReaders.Values)
            {
                memlib.CloseProcess();
            }
        }
        private Mem GetMemoryReader(uint processId)
        {
            Mem memlib;
            if (_memReaders.TryGetValue(processId, out memlib)) return memlib;
            _memReaders[processId] = new Mem();
            bool suc = _memReaders[processId].OpenProcess((int)processId, out var failReason);
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

        public void WindowEventHandler(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            _logger.Information("Foreground window changed");
            _conf = Config.GetInstance();
            ForegroundProcess fp = new ForegroundProcess();
            GameProfile matchedGp = _conf.GameProfiles.FirstOrDefault(x => x.Matches(fp));
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
                    catch {
                        return;
                    }
                    
                    _logger.Debug("Starting thread to watch memory for changes.");
                    new Thread(() =>
                    {
                        while (true)
                        {
                            int value = memlib.ReadByte(matchedGp.Memscan.Code);
                            if (matchedGp.Memscan.Match.TryGetValue(value, out var profName))
                            {

                                matchedRp = _conf.RecoilProfiles.FirstOrDefault(p => p.Name == profName);
                                if (matchedRp == null)
                                {
                                    _logger.Error(
                                        "[{@Game}][MEM] {@Value} -> {@Profile} not found. Check your configuration.",
                                        matchedGp.Name, value, profName);
                                    return;
                                }
                            }
                            else
                            {
                                _logger.Error("[{@Game}][MEM] {@Value} -> No profile found. Check your configuration.",
                                    matchedGp.Name, value);
                                return;
                            }

                            if (matchedRp != _currentProfile)
                            {
                                _logger.Information("[{@Game}][MEM] {@Value} -> {@Profile}", matchedGp.Name, value,
                                    matchedRp.Name);
                                _server.SendMessage(MessageBuilder.Build("profile", matchedRp).AsMessage());
                                if (_conf.Global.RecoilOnSwitch)
                                {
                                    _server.SendMessage(MessageBuilder.Build("recoil", null).AsMessage());
                                }

                                _currentProfile = matchedRp;
                            }

                            ForegroundProcess newFp = new ForegroundProcess();
                            if (newFp.ProcessId != fp.ProcessId)
                            {
                                _logger.Information("Detected window swap during memory scan, terminating thread.");
                                return;
                            }

                            Thread.Sleep(500);
                        }
                    }).Start();
                }
                else // Single profile swap
                {
                    matchedRp = _conf.RecoilProfiles.FirstOrDefault(p => p.Name == matchedGp.Profile);
                    if (matchedRp == null)
                    {
                        _logger.Error("Could not find any profile named {@Profile}. Check your configuration.", matchedGp.Profile);
                        return;
                    }
                    if (matchedRp != _currentProfile)
                    {
                        _logger.Information($"Matched profile {matchedGp.Name}, requesting switch to {matchedGp.Profile}");
                        _server.SendMessage(MessageBuilder.Build("profile", matchedRp).AsMessage());
                        if (_conf.Global.RecoilOnSwitch)
                        {
                            _server.SendMessage(MessageBuilder.Build("recoil", null).AsMessage());
                        }
                        _currentProfile = matchedRp;
                    }
                }
            }
        }
        public void MessageHandler(List<string> messages)
        {
            foreach (var e in from msg in messages where !string.IsNullOrEmpty(msg) select MessageBuilder.FromMessage(msg))
            {
                switch (e.type)
                {
                    case "recoilack":
                        if (!(bool)e.payload)
                        {
                            _logger.Error($"Failed to recoil");
                        }
                        else
                        {
                            _logger.Information($"Recoil ACK - Success");
                        }
                        break;
                    case "profileack":
                        var suc = (bool)e.payload;
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
    }
    internal class Program
    {
        static WinEventDelegate dele = null;

        delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_SYSTEM_FOREGROUND = 3;

        [STAThread]
        static void Main(string[] args)
        {
            Config conf = Config.GetInstance();
            AppForm mainForm = new AppForm();
            ILogger logger = SindenCompanionShared.Logger.CreateDesktopLogger(conf.Global.Debug, mainForm.WpfRichTextBox);
            App app = new App(logger);

            SindenInjector injector = new SindenInjector(conf.Global.Lightgun, logger);
            injector.Inject();

            dele = new WinEventDelegate(app.WindowEventHandler);
            IntPtr m_hhook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, dele, 0, 0, WINEVENT_OUTOFCONTEXT);
            Application.ApplicationExit += new EventHandler((s, a) =>
            {
                injector.Dispose();
                app.Dispose();
            });
            Application.EnableVisualStyles();
            Application.Run(mainForm);
        }
    }
}
