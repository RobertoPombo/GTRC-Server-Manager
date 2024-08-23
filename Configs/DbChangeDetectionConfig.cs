using Newtonsoft.Json;
using System.IO;
using System.Text;

using GTRC_Basics;

namespace GTRC_Server_Bot.Configs
{
    public class DbChangeDetectionConfig
    {
        private static readonly string path = GlobalValues.ConfigDirectory + "config database change detection.json";

        private int intervallMin = 0;

        public int IntervallMin
        {
            get { return intervallMin; }
            set { intervallMin = Math.Min(1440, Math.Max(1, value)); }
        }

        public bool IsActive { get; set; } = false;

        public static DbChangeDetectionConfig LoadJson()
        {
            DbChangeDetectionConfig? registrationsReportBotConfig = null;
            if (!File.Exists(path)) { File.WriteAllText(path, JsonConvert.SerializeObject(new DbChangeDetectionConfig(), Formatting.Indented), Encoding.Unicode); }
            try
            {
                registrationsReportBotConfig = JsonConvert.DeserializeObject<DbChangeDetectionConfig>(File.ReadAllText(path, Encoding.Unicode));
                GlobalValues.CurrentLogText = "Reload database bot settings restored.";
            }
            catch { GlobalValues.CurrentLogText = "Restore reload database bot settings failed!"; }
            registrationsReportBotConfig ??= new DbChangeDetectionConfig();
            return registrationsReportBotConfig;
        }

        public static void SaveJson(DbChangeDetectionConfig registrationsReportBotConfig)
        {
            string text = JsonConvert.SerializeObject(registrationsReportBotConfig, Formatting.Indented);
            File.WriteAllText(path, text, Encoding.Unicode);
            GlobalValues.CurrentLogText = "Reload database bot settings saved.";
        }
    }
}
