using System;
using System.Diagnostics;
using System.IO;
using HoLLy.ManagedInjector;
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
                //newProcess.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
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

            } else if (localByName.Length == 0)
            {
                _logger.Error("Could not find Lightgun.exe - start it before or specify path in config file.");
                throw new Exception("Lightgun.exe not found");
            } else
            {
                process = localByName[0];
            }
        }

        public void Dispose() {
            if (process != null && _started)
            {
                process.Kill();
            }
        }

        public void Inject()
        {
            _logger.Information("Injecting library ({@Library}) in process {@PID}", libraryPath, process.Id);
            var injector = new InjectableProcess((uint)process.Id);
            injector.Inject(libraryPath, "SindenHook.EntryPoint", "Run");
            _logger.Information("Done injecting");
        }
    }
}

