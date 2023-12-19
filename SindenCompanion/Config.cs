using System.Collections.Generic;
using System.IO;
using System.Text;
using SindenCompanionShared;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using System;

namespace SindenCompanion

{   public class Config
    {
        private static Config _instance = null;
        private static FileSystemWatcher _fw = null;
        public static Config GetInstance()
        {
            if (_instance != null) {
                return _instance;
            }
            if (_fw == null) { 
                _fw = new FileSystemWatcher();
                _fw.Path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                _fw.Filter = "config.yaml";
                _fw.Changed += new FileSystemEventHandler((e, a) => {
                    Console.WriteLine("Detected change to config file.");
                    _instance = null; 
                });
                _fw.EnableRaisingEvents = true;
            }

            Console.WriteLine("Loading configuration.");
            using (StreamReader streamReader = new StreamReader(".\\config.yaml", Encoding.UTF8))
            {
                try
                {
                    var deserializer = new DeserializerBuilder()
                                            .WithNamingConvention(UnderscoredNamingConvention.Instance)
                                            .Build();
                    _instance = deserializer.Deserialize<Config>(streamReader.ReadToEnd());
                } catch (Exception ex)
                {
                    Console.WriteLine($"Failed to instantiate configuration: {ex}");
                    throw ex;
                }
            }
            return _instance;
        }

        public List<RecoilProfile> RecoilProfiles { get; set; }
        public List<GameProfile> GameProfiles { get; set; }

        public Global Global { get; set; }
        public Config()
        {
            RecoilProfiles = new List<RecoilProfile>();
            GameProfiles = new List<GameProfile>();
        }
    } 

    public class Global
    {
        public bool RecoilOnSwitch { get; set; }
        public int IpcPort { get; set; }
        public string Lightgun {  get; set; }

        public bool Debug { get; set; }

        public Global()
        {
            IpcPort = 5557;
        }
    }
    public class GameProfile
    {
        public string Name { get; set; }

        public string Profile { get; set; }

        public GameMatchRule Match { get; set; }

        public MemScan Memscan { get; set; }

        public bool Matches (ForegroundProcess fp)
        {
            // If both exe and title are set, AND them
            if (Match.Exe != null && Match.Title != null)
            {
                if ($"{fp.ProcessName}.exe" == Match.Exe && Match.Title == fp.WindowTitle)
                {
                    return true;
                }
                return false;
            }
            
            if ($"{fp.ProcessName}.exe" == Match.Exe)
            {
                return true;
            }

            if (Match.Title == fp.WindowTitle)
            {
                return true;
            }

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
