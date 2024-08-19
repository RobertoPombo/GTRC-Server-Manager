using Newtonsoft.Json;
using System.IO;
using System.Text;

using GTRC_Basics;

namespace GTRC_Server_Bot.Configs
{
    public class RegistrationsReportBotConfig
    {
        private static readonly string path = GlobalValues.DataDirectory + "config registrations report bot.json";

        private int intervallMin = 0;

        public int IntervallMin
        {
            get { return intervallMin; }
            set { intervallMin = Math.Min(1440, Math.Max(1, value)); }
        }

        public bool IsActive { get; set; } = false;

        public static RegistrationsReportBotConfig LoadJson()
        {
            RegistrationsReportBotConfig? registrationsReportBotConfig = null;
            if (!Directory.Exists(GlobalValues.DataDirectory)) { Directory.CreateDirectory(GlobalValues.DataDirectory); }
            if (!File.Exists(path)) { File.WriteAllText(path, JsonConvert.SerializeObject(new RegistrationsReportBotConfig(), Formatting.Indented), Encoding.Unicode); }
            try
            {
                registrationsReportBotConfig = JsonConvert.DeserializeObject<RegistrationsReportBotConfig>(File.ReadAllText(path, Encoding.Unicode));
                GlobalValues.CurrentLogText = "Registrations report bot settings restored.";
            }
            catch { GlobalValues.CurrentLogText = "Restore registrations report bot settings failed!"; }
            registrationsReportBotConfig ??= new RegistrationsReportBotConfig();
            return registrationsReportBotConfig;
        }

        public static void SaveJson(RegistrationsReportBotConfig registrationsReportBotConfig)
        {
            string text = JsonConvert.SerializeObject(registrationsReportBotConfig, Formatting.Indented);
            File.WriteAllText(path, text, Encoding.Unicode);
            GlobalValues.CurrentLogText = "Registrations report bot settings saved.";
        }
    }
}
