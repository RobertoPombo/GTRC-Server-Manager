using Newtonsoft.Json;
using System.IO;
using System.Text;

using GTRC_Basics;

namespace GTRC_Server_Bot.Configs
{
    public class ReloadDatabaseBotConfig
    {
        private static readonly string path = GlobalValues.ConfigDirectory + "config reload database bot.json";

        private int intervallMin = 0;

        public int IntervallMin
        {
            get { return intervallMin; }
            set { intervallMin = Math.Min(1440, Math.Max(1, value)); }
        }

        public bool IsActive { get; set; } = false;

        public static ReloadDatabaseBotConfig LoadJson()
        {
            ReloadDatabaseBotConfig? registrationsReportBotConfig = null;
            if (!File.Exists(path)) { File.WriteAllText(path, JsonConvert.SerializeObject(new ReloadDatabaseBotConfig(), Formatting.Indented), Encoding.Unicode); }
            try
            {
                registrationsReportBotConfig = JsonConvert.DeserializeObject<ReloadDatabaseBotConfig>(File.ReadAllText(path, Encoding.Unicode));
                GlobalValues.CurrentLogText = "Reload database bot settings restored.";
            }
            catch { GlobalValues.CurrentLogText = "Restore reload database bot settings failed!"; }
            registrationsReportBotConfig ??= new ReloadDatabaseBotConfig();
            return registrationsReportBotConfig;
        }

        public static void SaveJson(ReloadDatabaseBotConfig registrationsReportBotConfig)
        {
            string text = JsonConvert.SerializeObject(registrationsReportBotConfig, Formatting.Indented);
            File.WriteAllText(path, text, Encoding.Unicode);
            GlobalValues.CurrentLogText = "Reload database bot settings saved.";
        }
    }
}
