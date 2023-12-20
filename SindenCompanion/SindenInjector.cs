using HoLLy.ManagedInjector;
using Microsoft.Diagnostics.Runtime;
using Newtonsoft.Json;
using Serilog;
using SindenHook;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;


namespace SindenCompanion
{
    internal class SindenInjector : IDisposable
    {
        private Process process;
        private string libraryPath;
        private ILogger _logger;
        private bool _started;
        private Config _config;
        private InjectionArguments _injectionArguments;

        public static string GetApplicationExecutableDirectoryName()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        private static Process CreateProcess(string path)
        {
            Process newProcess = new Process();
            newProcess.StartInfo = new ProcessStartInfo(path);
            newProcess.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            newProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(path);
            return newProcess;
        }
        public SindenInjector(Config config, ILogger logger)
        {
            _config = config;
            _logger = logger;
            libraryPath = Path.Combine(GetApplicationExecutableDirectoryName(), "SindenHook.dll");
            _logger.Information("Looking for Lightgun.exe");
            Process[] localByName = Process.GetProcessesByName("Lightgun");
            if (localByName.Length == 0 && !string.IsNullOrEmpty(_config.Global.Lightgun))
            {
                _logger.Warning("Could not find Lightgun.exe, starting it now. {@Path}", _config.Global.Lightgun);
                Process newProcess = CreateProcess(_config.Global.Lightgun);
                try
                {
                    newProcess.Start();
                    process = newProcess;
                } catch (Exception ex) {
                    _logger.Error("Failed to start Lightgun.exe {@Exc}", ex);
                    throw ex;
                }
                _started = true;
                // Give it some time to initialize the main form...
                Thread.Sleep(2000);

            } else if (localByName.Length == 0)
            {
                var exePath = Path.Combine(GetApplicationExecutableDirectoryName(), "Lightgun.exe");
                if (File.Exists(exePath))
                {
                    _logger.Information("Found Lightgun.exe in current working directory");
                    Process newProcess = CreateProcess(exePath);
                    try
                    {
                        newProcess.Start();
                        process = newProcess;
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
            } else
            {
                process = localByName[0];
            }
            _injectionArguments = new InjectionArguments
            {
                CommunicationPort = _config.Global.IpcPort
            };
        }
        

        public bool IsAlive()
        {
            return process != null && !process.HasExited;
        }
        public void Dispose() {
            if (process != null && _started)
            {
                try
                {
                    process.Kill();
                }
                catch (Exception ex)
                {
                    _logger.Error("Failed to kill Lightgun.exe {@Exc}", ex);
                }
            }
        }

        public void Inject()
        {
            var dt = DataTarget.AttachToProcess(process.Id, false);
            var dllAlreadyLoaded = dt
                .ClrVersions
                .Select(dtClrVersion => dtClrVersion.CreateRuntime())
                .SelectMany(runtime => runtime.AppDomains.SelectMany(runtimeAppDomain => runtimeAppDomain.Modules))
                .Select(clrModule => clrModule.AssemblyName)
                .Distinct()
                .ToList().Any(x => x == libraryPath);
            if (dllAlreadyLoaded)
            {
               _logger.Error("Library already loaded in process {@PID}, will not reinject. If this causes issues, restart Lightgun.exe", process.Id);
                return;
            }
            _logger.Information("Injecting library ({@Library}) in process {@PID}", libraryPath, process.Id);
            var injector = new InjectableProcess((uint)process.Id);
            var argsJson = JsonConvert.SerializeObject(_injectionArguments);
            injector.Inject(libraryPath, "SindenHook.EntryPoint", "Run", argsJson);
            _logger.Information("Done injecting");
        }
    }
}

