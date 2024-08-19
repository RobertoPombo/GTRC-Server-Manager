using System.IO;
using System.Net;
using Newtonsoft.Json;
using System.Text;

using GTRC_Basics;
using GTRC_Basics.Models;
using GTRC_Database_Client;
using GTRC_Database_Client.Responses;
using GTRC_Server_Basics.Discord;

namespace GTRC_Server_Bot.Scripts
{
    public class RegistrationsReport
    {
        private static readonly string pathPrefix = GlobalValues.DataDirectory + "list entries - seasonId ";
        private static readonly string pathSuffix = ".json";

        private int seasonId = GlobalValues.NoId;
        private List<Entry> currentEntries = [];
        private string path { get { return pathPrefix + seasonId.ToString() + pathSuffix; } }

        public RegistrationsReport()
        {
            LoadListEntries();
        }

        public async Task UpdateListEntries(Season season)
        {
            seasonId = season.Id;
            LoadListEntries();
            DbApiListResponse<Entry> respListEnt = await DbApi.DynCon.Entry.GetChildObjects(typeof(Season), season.Id);
            if (respListEnt.Status == HttpStatusCode.OK)
            {
                List<Entry> newEntries = [];
                foreach (Entry entry1 in respListEnt.List)
                {
                    bool isInCurrentList = false;
                    foreach (Entry entry2 in currentEntries) { if (entry1.Id == entry2.Id) { isInCurrentList = true; break; } }
                    if (!isInCurrentList) { newEntries.Add(entry1); }
                }
                currentEntries = [];
                foreach (Entry entry1 in respListEnt.List) { currentEntries.Add(entry1); }
                SaveListEntries();
                if (newEntries.Count > 0)
                {
                    await DiscordNotifications.ShowNewRegistrations(season, newEntries);
                    DbApiObjectResponse<Event> respObjEve = await DbApi.DynCon.Event.GetNext(season.Id);
                    if (respObjEve.Status == HttpStatusCode.OK) { await DiscordNotifications.ShowEntrylist(respObjEve.Object); }
                }
            }
            GlobalValues.CurrentLogText = "Registration report sent.";
        }

        private void LoadListEntries()
        {
            if (!Directory.Exists(GlobalValues.DataDirectory)) { Directory.CreateDirectory(GlobalValues.DataDirectory); }
            if (!File.Exists(path)) { File.WriteAllText(path, JsonConvert.SerializeObject(new List<Entry>(), Formatting.Indented), Encoding.Unicode); }
            try
            {
                currentEntries = JsonConvert.DeserializeObject<List<Entry>>(File.ReadAllText(path, Encoding.Unicode)) ?? [];
                GlobalValues.CurrentLogText = "List of registrations loaded.";
            }
            catch { GlobalValues.CurrentLogText = "Load list of registrations failed!"; }
        }

        private void SaveListEntries()
        {
            string text = JsonConvert.SerializeObject(currentEntries, Formatting.Indented);
            File.WriteAllText(path, text, Encoding.Unicode);
        }
    }
}
