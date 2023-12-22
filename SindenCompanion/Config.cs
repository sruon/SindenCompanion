using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using FluentValidation;
using Serilog;
using SindenCompanionShared;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SindenCompanion
{
    public class RecoilProfileValidator : AbstractValidator<RecoilProfile>
    {
        public RecoilProfileValidator(Config parent)
        {
            RuleFor(x => x.Name).NotNull().NotEmpty().WithMessage("Recoil profile name is required.")
                .Must(x => UniqueName(parent, x)).WithMessage(x => $"RecoilProfile.{x.Name} Duplicate profile names.");
            // "Pulse Length (Strength)" 40-80, default value is 60
            RuleFor(x => x.PulseLength).NotNull().InclusiveBetween(40, 80)
                .WithMessage(profile =>
                    $"RecoilProfile.{profile.Name}.PulseLength must be between 40 and 80 (inclusive).");
            ;
            // "Delay Between Pulses (Speed)" 0(Fast)-50(Slow), default value is 10
            RuleFor(x => x.DelayBetweenPulses).NotNull().InclusiveBetween(0, 50)
                .WithMessage(profile =>
                    $"RecoilProfile.{profile.Name}.DelayBetweenPulses must be between 0 and 50 (inclusive).");
            ;
            // "Extra Delay After First Pulse" 0-16, default value is 0
            RuleFor(x => x.DelayAfterFirstPulse).NotNull().InclusiveBetween(0, 16)
                .WithMessage(profile =>
                    $"RecoilProfile.{profile.Name}.DelayAfterFirstPulse must be between 0 and 16 (inclusive).");
            ;
            // "Recoil Strength (Voltage)" 0(weakest)-10, default value is 10
            RuleFor(x => x.Strength).NotNull().InclusiveBetween(0, 10)
                .WithMessage(profile => $"RecoilProfile.{profile.Name}.Strength must be between 0 and 10 (inclusive).");
            ;
        }

        private bool UniqueName(Config conf, string name)
        {
            var matching = conf.RecoilProfiles
                .Count(x => x.Name.ToLower() == name.ToLower());

            return matching <= 1;
        }
    }

    public class ConfigValidator : AbstractValidator<Config>
    {
        public ConfigValidator()
        {
            RuleFor(x => x.Global).NotNull();
            RuleFor(x => x.Global.IpcPort).InclusiveBetween(1025, 65535);
            RuleFor(x => x.Global.Lightgun).Must(LightgunPathExists)
                .WithMessage("Global.Lightgun The path provided for Lightgun.exe does not exist.")
                .When(x => !string.IsNullOrEmpty(x.Global.Lightgun));
            RuleForEach(x => x.GameProfiles).SetValidator(parent => new GameProfileValidator(parent));
            RuleForEach(x => x.RecoilProfiles).SetValidator(parent => new RecoilProfileValidator(parent));
        }

        private bool LightgunPathExists(string path)
        {
            return File.Exists(path);
        }
    }

    public class Config
    {
        private static Config _instance;
        private static FileSystemWatcher _fw;
        public static ILogger Logger = null;
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
                    if (Logger != null) Logger.Information("Detected changes to config file.");
                    _instance = null;
                });
                _fw.EnableRaisingEvents = true;
            }

            if (Logger != null)
                Logger.Information("Loading configuration.");

            using (var streamReader = new StreamReader(".\\config.yaml", Encoding.UTF8))
            {
                try
                {
                    var deserializer = new DeserializerBuilder()
                        .WithNamingConvention(UnderscoredNamingConvention.Instance)
                        .Build();
                    _instance = deserializer.Deserialize<Config>(streamReader.ReadToEnd());
                    var validator = new ConfigValidator();
                    var result = validator.Validate(_instance);
                    if (!result.IsValid)
                    {
                        var reason = "";
                        foreach (var failure in result.Errors)
                            reason += "Property " + failure.PropertyName + " failed validation. Error was: " +
                                      failure.ErrorMessage + "\n";
                        if (Logger != null)
                            Logger.Error($"Invalid configuration file: {result.Errors.Count} errors: {reason}");
                        throw new Exception($"Invalid configuration file: {result.Errors.Count} errors: {reason}");
                    }
                }
                catch (Exception ex)
                {
                    if (Logger != null)
                        Logger.Error($"Failed to instantiate configuration: {ex}");
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

        public bool OffscreenReload { get; set; } = false;

        public string InputType { get; set; } = "joystick";

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

            if (!string.IsNullOrEmpty(Match.Title) && Match.Title == fp.WindowTitle) return true;

            return false;
        }
    }

    public enum ScanType
    {
        Byte = 1,
        Short = 2,
        Int = 3,
        UInt = 4,
        Float = 5,
        Long = 6,
        Double = 7,
        String = 8
    }


    public class GameProfileValidator : AbstractValidator<GameProfile>
    {
        public GameProfileValidator(Config parent)
        {
            // Uniqueness does not really matter for Game Profiles
            RuleFor(x => x.Name).NotNull().NotEmpty();
            RuleFor(x => x.Profile).NotNull().NotEmpty().Must(x => parent.RecoilProfiles.Any(p => p.Name == x))
                .WithMessage(x => $"GameProfile.{x.Name}.Profile: Profile {x.Profile} does not exist.");
            RuleFor(x => x.Match).NotNull().WithMessage(x => $"GameProfile.{x.Name}.Match cannot be empty/missing.");
            RuleFor(x => x.Match.Exe).NotNull().NotEmpty().When(x => x.Match != null && x.Match.Title == null)
                .WithMessage(x => $"GameProfile.{x.Name}.Match.Exe must be set when title is empty.");
            RuleFor(x => x.Match.Title).NotNull().NotEmpty().When(x => x.Match != null && x.Match.Exe == null)
                .WithMessage(x => $"GameProfile.{x.Name}.Match.Title must be set when exe is empty.");
            RuleFor(x => x.Memscan.Paths).NotNull().NotEmpty().Must(x => x.Length <= 2).When(x => x.Memscan != null)
                .WithMessage("At least one pointer path must be provided. At most two pointer paths can be provided.");
            RuleFor(x => x.Memscan.Type).NotNull().Must(ValidScanType).When(x => x.Memscan != null).WithMessage(x =>
                $"GameProfile.{x.Name}.Memscan.Type: A valid type must be provided ({string.Join(", ", MemScan.TypeMap.Keys)})");
            RuleFor(x => x.Memscan.Match).NotNull().NotEmpty().When(x => x.Memscan != null)
                .WithMessage("A dictionary (value/profile name) must be provided");
            RuleFor(x => x.InputType).Must(x => x == "mouse" || x == "joystick")
                .WithMessage($"GameProfile.{x.Name}.InputType must be either 'mouse' or 'joystick'");
        }

        public bool ValidScanType(string scantype)
        {
            return MemScan.TypeMap.Keys.Contains(scantype);
        }
    }

    public class GameMatchRule
    {
        public string Exe { get; set; }
        public string Title { get; set; }
    }

    public class MemScan
    {
        public static readonly Dictionary<string, ScanType> TypeMap = new Dictionary<string, ScanType>()
        {
            { "byte", ScanType.Byte },
            { "short", ScanType.Short },
            { "int", ScanType.Int },
            { "uint", ScanType.UInt }
            //{"float", ScanType.Float},
            //{"long", ScanType.Long},
            //{"double", ScanType.Double},
            //{"string", ScanType.String},
        };

        public string[] Paths { get; set; }
        public string Type { get; set; }

        public Dictionary<int, string> Match { get; set; }
    }

    //public class MemMatchContainer<T>
    //{
    //    private List<Tuple<T, string>> _matches = new List<Tuple<T, string>>();

    //    private Dictionary<T, string> _matchesDict = new Dictionary<T, string>();

    //    public void Add(T key, string value)
    //    {
    //        Type itemType = typeof(T);

    //        if (itemType == typeof(int) || itemType == typeof(short) || itemType == typeof(uint) || itemType == typeof(byte) || itemType == typeof(string))
    //        {
    //            _matchesDict.Add(key, value);
    //        }
    //        else if (itemType == typeof(double) || itemType == typeof(float) || itemType == typeof(long))
    //        {
    //            _matches.Add(new Tuple<T, string>(key, value));
    //        }
    //    }

    //    public string GetMatch(T key)
    //    {
    //        Type itemType = typeof(T);
    //        if (itemType == typeof(int) || itemType == typeof(short) || itemType == typeof(uint) || itemType == typeof(byte) || itemType == typeof(string))
    //        {
    //            _matchesDict.TryGetValue(key, out string ret);
    //            return ret;
    //        }
    //        //if (itemType == typeof(double) || itemType == typeof(float) || itemType == typeof(long))
    //        //{
    //        //    // probably horribly bugged, revisit later
    //        //    var closest = _matches.OrderBy(v => Math.Abs(Convert.ToDouble(v.Item1) - Convert.ToDouble(key))).First();
    //        //}

    //        //return null;
    //        return "";
    //    }
    //}
}