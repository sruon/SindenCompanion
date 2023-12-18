using System;
using SindenCompanionShared;
using System.Linq;
using System.Collections.Generic;
using Serilog;
using Memory;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;

namespace SindenCompanion
{
    public class App
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

        private Mem GetMemoryReader(uint processId)
        {
            Mem Memlib;
            if (!_memReaders.TryGetValue(processId, out Memlib))
            {
                string failReason;
                _memReaders[processId] = new Mem();
                bool suc = _memReaders[processId].OpenProcess((int)processId, out failReason);
                if (!suc)
                {
                    _logger.Error("Failed to open process for memory reading: {@failReason}", failReason);
                    throw new Exception("Failed to open process for memory reading");
                }
                else
                {
                    _logger.Information("Successfully opened process for memory reading");
                    Memlib = _memReaders[processId];
                }
            }
            return Memlib;
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
                    Mem Memlib;

                    try
                    {
                        Memlib = GetMemoryReader(fp.ProcessId);
                    }
                    catch {
                        return;
                    }
                    
                    while (true)
                    {
                        int value = Memlib.ReadByte(matchedGp.Memscan.Code);
                        string profName;
                        if (matchedGp.Memscan.Match.TryGetValue(value, out profName))
                        {
                            matchedRp = _conf.RecoilProfiles.FirstOrDefault(p => p.Name == profName);
                            if (matchedRp == null)
                            {
                                _logger.Error("Could not find any profile named {@Profile}. Check your configuration.", profName);
                                return;
                            }
                        }
                        else
                        {
                            _logger.Error("Could not match value {@Value} to a profile. Check your configuration.", value);
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
                        ForegroundProcess newFp = new ForegroundProcess();
                        if (newFp.ProcessId != fp.ProcessId)
                        {
                            _logger.Information("Detected window swap during memory scan, breaking loop.");
                            return;
                        }
                        Thread.Sleep(500);
                    }
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
            foreach (string msg in messages)
            {
                if (msg != null && msg != string.Empty)
                {
                    Enveloppe e = MessageBuilder.FromMessage(msg);
                    switch (e.type)
                    {
                        case "recoilack":
                            var recsuc = (bool)e.payload;
                            if (!recsuc)
                            {
                                _logger.Error($"Failed to recoil");
                            }
                            else
                            {
                                _logger.Information($"Recoil ACK - Success {(bool)e.payload}");
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
                        default:
                            break;
                    }
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
            Application.EnableVisualStyles();
            Application.Run(mainForm);
        }
    }
}
