using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using HoLLy.ManagedInjector;
using Memory;
using Microsoft.Diagnostics.Runtime;
using Serilog;


namespace SindenCompanion
{
    internal class SindenInjector : IDisposable
    {
        private Process process;
        private string libraryPath;
        private ILogger _logger;
        private bool _started;
        public SindenInjector(string lightgunPath, ILogger logger)
        {
            _logger = logger;
            libraryPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "SindenHook.dll");
            _logger.Information("Looking for Lightgun.exe");
            Process[] localByName = Process.GetProcessesByName("Lightgun");
            if (localByName.Length == 0 && lightgunPath != string.Empty)
            {
                _logger.Warning("Could not find Lightgun.exe, starting it now. {@Path}", lightgunPath);
                Process newProcess = new Process();
                newProcess.StartInfo = new ProcessStartInfo(lightgunPath);
                newProcess.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                newProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(lightgunPath);
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
                _logger.Error("Could not find Lightgun.exe - start it before or specify path in config file.");
                throw new Exception("Lightgun.exe not found");
            } else
            {
                process = localByName[0];
            }
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
            injector.Inject(libraryPath, "SindenHook.EntryPoint", "Run");
            _logger.Information("Done injecting");
        }
    }
}

