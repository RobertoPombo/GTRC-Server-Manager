using Newtonsoft.Json;
using System.IO;
using System.Text;

using GTRC_Basics;

namespace GTRC_Server_Bot.Configs
{
    public class DbChangeDetectionConfig
    {
        public static readonly int MinIntervallMin = 1;
        public static readonly int MaxIntervallMin = 1440;
        private static readonly string path = GlobalValues.ConfigDirectory + "config database change detection.json";

        private int intervallMin = MinIntervallMin;

        public int IntervallMin
        {
            get { return intervallMin; }
            set { intervallMin = Math.Min(MaxIntervallMin, Math.Max(MinIntervallMin, value)); }
        }

        public bool IsActive { get; set; } = false;

        public static DbChangeDetectionConfig LoadJson()
        {
            DbChangeDetectionConfig? config = null;
            if (!File.Exists(path)) { File.WriteAllText(path, JsonConvert.SerializeObject(new DbChangeDetectionConfig(), Formatting.Indented), Encoding.Unicode); }
            try
            {
                config = JsonConvert.DeserializeObject<DbChangeDetectionConfig>(File.ReadAllText(path, Encoding.Unicode));
                GlobalValues.CurrentLogText = "Reload database bot settings restored.";
            }
            catch { GlobalValues.CurrentLogText = "Restore reload database bot settings failed!"; }
            config ??= new DbChangeDetectionConfig();
            return config;
        }

        public static void SaveJson(DbChangeDetectionConfig config)
        {
            string text = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(path, text, Encoding.Unicode);
            GlobalValues.CurrentLogText = "Database bot settings saved.";
        }
    }
}
