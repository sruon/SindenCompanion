using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using SindenCompanionShared;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SindenCompanion

{
    public class Config
    {
        private static Config _instance;
        private static FileSystemWatcher _fw;

        public List<RecoilProfile> RecoilProfiles { get; set; } = new List<RecoilProfile>();
        public List<GameProfile> GameProfiles { get; set; } = new List<GameProfile>();

        public Global Global { get; set; }

        public static Config GetInstance()
        {
            if (_instance != null) return _instance;
            if (_fw == null)
            {
                _fw = new FileSystemWatcher();
                _fw.Path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                _fw.Filter = "config.yaml";
                _fw.Changed += new FileSystemEventHandler((e, a) =>
                {
                    Console.WriteLine("Detected change to config file.");
                    _instance = null;
                });
                _fw.EnableRaisingEvents = true;
            }

            Console.WriteLine("Loading configuration.");
            using (var streamReader = new StreamReader(".\\config.yaml", Encoding.UTF8))
            {
                try
                {
                    var deserializer = new DeserializerBuilder()
                        .WithNamingConvention(UnderscoredNamingConvention.Instance)
                        .Build();
                    _instance = deserializer.Deserialize<Config>(streamReader.ReadToEnd());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to instantiate configuration: {ex}");
                    throw ex;
                }
            }

            return _instance;
        }
    }


    public class Global
    {
        public Global()
        {
            IpcPort = 5557;
        }

        public bool RecoilOnSwitch { get; set; }
        public int IpcPort { get; set; }
        public string Lightgun { get; set; }

        public bool Debug { get; set; }
    }

    public class GameProfile
    {
        public string Name { get; set; }

        public string Profile { get; set; }

        public GameMatchRule Match { get; set; }

        public MemScan Memscan { get; set; }

        public bool Matches(ForegroundProcess fp)
        {
            // If both exe and title are set, AND them
            if (Match.Exe != null && Match.Title != null)
            {
                if ($"{fp.ProcessName}.exe" == Match.Exe && Match.Title == fp.WindowTitle) return true;
                return false;
            }

            if ($"{fp.ProcessName}.exe" == Match.Exe) return true;

            if (Match.Title == fp.WindowTitle) return true;

            return false;
        }
    }

    public class GameMatchRule
    {
        public string Exe { get; set; }
        public string Title { get; set; }
    }

    public class MemScan
    {
        public string Code { get; set; }
        public byte Size { get; set; }

        public Dictionary<int, string> Match { get; set; }
    }
}