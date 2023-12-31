﻿using System;
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
        private bool _clientReady;
        private Config _conf;
        private List<RecoilProfile> _currentProfile = new List<RecoilProfile> { null, null };

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

        public void ChangeProfile(int playerIndex, RecoilProfile rp)
        {
            var rpw = new RecoilProfileWrapper
            {
                RecoilProfile = rp,
                Player = playerIndex
            };
            _server.BuildAndSendMessage("profile", rpw);
            if (_conf.Global.RecoilOnSwitch)
                _server.BuildAndSendMessage("recoil", playerIndex);
            if (playerIndex == -1)
                _currentProfile = new List<RecoilProfile> { rp, rp };
            else
                _currentProfile[playerIndex] = rp;
        }

        public void InjectionNotification()
        {
            _server.BuildAndSendMessage("serverready", null);
        }

        private Mem GetMemoryReader(uint processId)
        {
            if (_memReaders.TryGetValue(processId, out var memlib))
            {
                if (memlib.MProc.MainModule == null) _memReaders[processId] = null;
                return memlib;
            }

            _memReaders[processId] = new Mem();
            var suc = _memReaders[processId].OpenProcess((int)processId, out var failReason);
            if (!suc)
            {
                _logger.Error("Failed to open process for memory reading: {@failReason}", failReason);
                throw new Exception("Failed to open process for memory reading");
            }

            _logger.Information("Successfully opened process for memory reading");
            memlib = _memReaders[processId];

            return memlib;
        }

        public void WindowEventHandler(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild,
            uint dwEventThread, uint dwmsEventTime)
        {
            var fp = new ForegroundProcess();
            _logger.Debug("[{@PID}][{@ProcName}][{@WindowTitle}]", fp.ProcessId, fp.ProcessName, fp.WindowTitle);
            if (!_clientReady)
            {
                _logger.Information("Sinden drivers not ready, ignoring window change");
                return;
            }

            _conf = Config.GetInstance();
            var matchedGp = _conf.GameProfiles.FirstOrDefault(x => x.Matches(fp));
            if (matchedGp == null) return;
            RecoilProfile matchedRp = null;
            if (matchedGp.Memscan != null) // Continuous swap based on memory
            {
                Mem memlib = null;

                while (memlib == null)
                    try
                    {
                        memlib = GetMemoryReader(fp.ProcessId);
                    }
                    catch
                    {
                        _logger.Error("Failed to open process for memory reading, retrying in 1s");
                        Thread.Sleep(1000);
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

                        var idx = 0;
                        foreach (var path in matchedGp.Memscan.Paths)
                        {
                            dynamic value;
                            string profName = null;
                            try
                            {
                                switch (matchedGp.Memscan.Type)
                                {
                                    case "byte":
                                        value = memlib.ReadByte(path);
                                        matchedGp.Memscan.Match.TryGetValue(value, out profName);
                                        break;
                                    case "short":
                                        value = memlib.Read2Byte(path);
                                        matchedGp.Memscan.Match.TryGetValue(value, out profName);
                                        break;
                                    case "int":
                                        value = memlib.ReadInt(path);
                                        matchedGp.Memscan.Match.TryGetValue(value, out profName);
                                        break;
                                    case "uint":
                                        value = memlib.ReadUInt(path);
                                        matchedGp.Memscan.Match.TryGetValue(value, out profName);
                                        break;
                                    case "dolphinbyte":
                                        value = memlib.ReadDolphinByte(path);
                                        matchedGp.Memscan.Match.TryGetValue(value, out profName);
                                        break;
                                    default:
                                        _logger.Error("Unsupported memory scan type: {@Type}",
                                            matchedGp.Memscan.Type);
                                        return;
                                }
                            }
                            catch (Exception e)
                            {
                                _logger.Error(e,
                                    "[P{@Index}] Exception while reading memory. The application may still be initializing or the pointer path is incorrect: {@Exception}",
                                    idx + 1, e);
                                continue;
                            }

                            _logger.Debug("[P{@Index}] Memory scan result: {@Value} -> {@Profile}", idx + 1, value,
                                profName);
                            if (!string.IsNullOrEmpty(profName))
                            {
                                matchedRp = _conf.RecoilProfiles.FirstOrDefault(p => p.Name == profName);
                                if (matchedRp == null)
                                {
                                    _logger.Error(
                                        "[{@Game}][MEM][Player {@Index}] {@Value} -> {@Profile} not found. Check your configuration.",
                                        matchedGp.Name, idx + 1, value, profName);
                                    continue;
                                }
                            }
                            else
                            {
                                _logger.Error(
                                    "[{@Game}][MEM][Player {@Index}] {@Value} -> No profile found. Check your configuration.",
                                    matchedGp.Name, idx + 1, value);
                                continue;
                            }

                            if (matchedRp != _currentProfile[idx])
                            {
                                _logger.Information("[{@Game}][MEM][Player {@Index}] {@Value} -> {@Profile}",
                                    matchedGp.Name, idx + 1, value,
                                    matchedRp.Name);
                                ChangeProfile(idx, matchedRp);
                            }

                            idx++;
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

                if (matchedRp == _currentProfile[0]) return;
                _logger.Information("[{@Game}] {@Profile}", matchedGp.Name, matchedRp.Name);
                ChangeProfile(-1, matchedRp);
            }
        }

        public void MessageHandler(List<string> messages)
        {
            foreach (var e in from msg in messages
                     where !string.IsNullOrEmpty(msg)
                     select MessageBuilder.FromString(msg))
                switch (e.Type)
                {
                    case "ready":
                        _logger.Information("Client signaled it's ready.");
                        _clientReady = true;
                        break;
                    case "recoilack":
                        var recoilResp = RecoilResponse.FromString(e.Payload.ToString());
                        if (!recoilResp.Success)
                            _logger.Error("Failed to recoil: {@Reason}", recoilResp.Reason);
                        else
                            _logger.Information($"Recoil success");
                        break;
                    case "profileack":
                        var resp = RecoilProfileResponse.FromString(e.Payload.ToString());

                        if (!resp.Success)
                            switch (resp.Reason)
                            {
                                case "No lightgun at requested index.":
                                    _logger.Error(
                                        $"Failed to apply profile -> No lightgun detected at requested player index");
                                    break;
                                default:
                                    _logger.Error("Failed to apply profile: {@Reason}", resp.Reason);
                                    break;
                            }
                        else
                            _logger.Information("Successfully applied profiles. {@Profile}", _currentProfile);

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
            Config conf;
            try
            {
                conf = Config.GetInstance();
            }
            catch (Exception e)
            {
                // Crash outright if we don't have a working config
                throw new InvalidOperationException($"Failed to read configuration: {e}");
            }

            var mainForm = new AppForm(conf);
            var logger = Logger.CreateDesktopLogger(conf.Global.Debug, mainForm.WpfRichTextBox);
            Config.Logger = logger;
            var app = new App(logger);
            mainForm.SetCallback(app.ChangeProfile);
            SindenInjector injector = null;
            new Thread(() =>
            {
                while (true)
                {
                    if (injector == null || !injector.IsAlive())
                    {
                        logger.Error("Lightgun.exe died, will attempt to restart. This will fail if path is not set.");
                        injector = new SindenInjector(conf, logger);
                        injector.Inject();
                        app.InjectionNotification();
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