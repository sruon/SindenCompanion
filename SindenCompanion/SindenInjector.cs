using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using HoLLy.ManagedInjector;
using Microsoft.Diagnostics.Runtime;
using Newtonsoft.Json;
using Serilog;
using SindenHook;

namespace SindenCompanion
{
    internal class SindenInjector : IDisposable
    {
        private readonly InjectionArguments _injectionArguments;
        private readonly string _libraryPath;
        private readonly ILogger _logger;
        private readonly Process _process;
        private readonly bool _started;

        public SindenInjector(Config config, ILogger logger)
        {
            _logger = logger;
            _libraryPath = Path.Combine(GetApplicationExecutableDirectoryName(), "SindenHook.dll");
            _logger.Information("Looking for Lightgun.exe");
            var localByName = Process.GetProcessesByName("Lightgun");
            if (localByName.Length == 0 && !string.IsNullOrEmpty(config.Global.Lightgun))
            {
                _logger.Warning("Could not find Lightgun.exe, starting it now. {@Path}", config.Global.Lightgun);
                var newProcess = CreateProcess(config.Global.Lightgun);
                try
                {
                    newProcess.Start();
                    _process = newProcess;
                }
                catch (Exception ex)
                {
                    _logger.Error("Failed to start Lightgun.exe {@Exc}", ex);
                    throw ex;
                }

                _started = true;
                // Give it some time to initialize the main form...
                Thread.Sleep(2000);
            }
            else if (localByName.Length == 0)
            {
                var exePath = Path.Combine(GetApplicationExecutableDirectoryName(), "Lightgun.exe");
                if (File.Exists(exePath))
                {
                    _logger.Information("Found Lightgun.exe in current working directory");
                    var newProcess = CreateProcess(exePath);
                    try
                    {
                        newProcess.Start();
                        _process = newProcess;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("Failed to start Lightgun.exe {@Exc}", ex);
                        throw ex;
                    }
                }
                else
                {
                    _logger.Error("Could not find Lightgun.exe - start it before or specify path in config file.");
                    throw new Exception("Lightgun.exe not found");
                }
            }
            else
            {
                _process = localByName[0];
            }

            _injectionArguments = new InjectionArguments
            {
                CommunicationPort = config.Global.IpcPort
            };
        }

        public void Dispose()
        {
            if (_process != null && _started)
                try
                {
                    _process.Kill();
                }
                catch (Exception ex)
                {
                    _logger.Error("Failed to kill Lightgun.exe {@Exc}", ex);
                }
        }

        public static string GetApplicationExecutableDirectoryName()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        private static Process CreateProcess(string path)
        {
            var newProcess = new Process();
            newProcess.StartInfo = new ProcessStartInfo(path);
            newProcess.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            newProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(path);
            return newProcess;
        }


        public bool IsAlive()
        {
            return _process != null && !_process.HasExited;
        }

        public void Inject()
        {
            var dt = DataTarget.AttachToProcess(_process.Id, false);
            var dllAlreadyLoaded = dt
                .ClrVersions
                .Select(dtClrVersion => dtClrVersion.CreateRuntime())
                .SelectMany(runtime => runtime.AppDomains.SelectMany(runtimeAppDomain => runtimeAppDomain.Modules))
                .Select(clrModule => clrModule.AssemblyName)
                .Distinct()
                .ToList().Any(x => x == _libraryPath);
            if (dllAlreadyLoaded)
            {
                _logger.Error(
                    "Library already loaded in process {@PID}, will not reinject. If this causes issues, restart Lightgun.exe",
                    _process.Id);
                return;
            }

            _logger.Information("Injecting library ({@Library}) in process {@PID}", _libraryPath, _process.Id);
            var injector = new InjectableProcess((uint)_process.Id);
            var argsJson = JsonConvert.SerializeObject(_injectionArguments);
            injector.Inject(_libraryPath, "SindenHook.EntryPoint", "Run", argsJson);
            _logger.Information("Done injecting");
        }
    }
}