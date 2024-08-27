using System.Net;

using GTRC_Basics;
using GTRC_Basics.Models;
using GTRC_Basics.Models.DTOs;
using GTRC_Basics.SimModels;
using GTRC_Database_Client;
using GTRC_Database_Client.Responses;

namespace GTRC_Server_Bot.Scripts
{
    public class ServerUpdate
    {
        private static readonly string AdminRoleName = "Rennleitung";

        public static async Task ExportEntrylistJson(Event _event, string path) //sollte Session sein todo temp, wirkt sich auf ForceEntrylist, ForceDriverInfo aus
        {
            AccEntrylist accEntrylist = new() { forceEntryList = 1 };       // sollte von Session abhängig sein
            List<EntryEvent> listEventEntries = [];
            List<Entry> listEntries = (await DbApi.DynCon.Entry.GetChildObjects(typeof(Season), _event.SeasonId)).List;
            foreach (Entry entry in listEntries)
            {
                DbApiObjectResponse<EntryEvent> respObjEntEve = await DbApi.DynCon.EntryEvent.GetAnyByUniqProps(new() { EntryId = entry.Id, EventId = _event.Id });
                if (respObjEntEve.Status == HttpStatusCode.OK) { listEventEntries.Add(respObjEntEve.Object); }
            }
            for (int index1 = 0; index1 < listEventEntries.Count - 1; index1++)
            {
                for (int index2 = index1; index2 < listEventEntries.Count; index2++)
                {
                    if (listEventEntries[index1].Entry.RaceNumber > listEventEntries[index2].Entry.RaceNumber)
                    {
                        (listEventEntries[index1], listEventEntries[index2]) = (listEventEntries[index2], listEventEntries[index1]);
                    }
                }
            }
            foreach (EntryEvent entryEvent in listEventEntries)
            {
                if (entryEvent.IsOnEntrylist || true) //nur wenn on Entrylist todo temp
                {
                    List<User> listDrivers = (await DbApi.DynCon.User.GetByEntryEvent(entryEvent.EntryId, _event.Id)).List;
                    if (listDrivers.Count > 0)
                    {
                        AccEntry accEntry = new();
                        bool isServerAdmin = false;
                        int forcedCarModel = -1;
                        DbApiObjectResponse<EntryDatetime> respObjEntDat = await DbApi.DynCon.EntryDatetime.GetAnyByUniqProps(new() { EntryId = entryEvent.EntryId, Date = _event.Date });
                        if (respObjEntDat.Status == HttpStatusCode.OK) { forcedCarModel = (int)respObjEntDat.Object.Car.AccCarId; }
                        foreach (User driver in listDrivers)
                        {
                            AccDriverCategory accDriverCategory = AccDriverCategory.Platinum;
                            if (!entryEvent.IsPointScorer) { accDriverCategory = AccDriverCategory.Silver; }
                            string name3Digits = EntryUserEvent.DefaultName3Digits;
                            EntryUserEventUniqPropsDto0 uniqDtoEntUseEve = new() { EntryId = entryEvent.EntryId, UserId = driver.Id, EventId = _event.Id };
                            DbApiObjectResponse<EntryUserEvent> respObjEntUseEve = await DbApi.DynCon.EntryUserEvent.GetByUniqProps(new() { Dto = uniqDtoEntUseEve });
                            if (respObjEntUseEve.Status == HttpStatusCode.OK) { name3Digits = respObjEntUseEve.Object.Name3Digits; }
                            accEntry.AddDriver(driver.SteamId, driver.FirstName, driver.LastName, name3Digits, (byte)accDriverCategory);

                            List<Role> listRoles = (await DbApi.DynCon.Role.GetByUser(driver.Id)).List;
                            foreach (Role role in listRoles) { if (role.Name == AdminRoleName) { isServerAdmin = true; break; } }
                        }
                        accEntrylist.AddEntry(accEntry.drivers, entryEvent.Entry.RaceNumber, isServerAdmin, true, forcedCarModel);
                    }
                }
            }
            int adminNr = 0;
            ushort adminRaceNumber = Entry.DefaultRaceNumber;
            Role adminRole = (await DbApi.DynCon.Role.GetByUniqProps(new() { Dto = new RoleUniqPropsDto0() { Name = AdminRoleName } })).Object;
            List<UserRole> listAdmins = (await DbApi.DynCon.UserRole.GetChildObjects(typeof(Role), adminRole.Id)).List;
            foreach (UserRole admin in listAdmins)      //todo temp hier fehlt die Überprüfung ob Admin auf Entrylist oder gebannt
            {
                adminNr++;
                adminRaceNumber++;
                bool isUsedRaceNumber = true;
                while (isUsedRaceNumber)
                {
                    isUsedRaceNumber = false;
                    foreach (EntryEvent entryEvent in listEventEntries)
                    {
                        if (entryEvent.Entry.RaceNumber == adminRaceNumber) { adminRaceNumber++; isUsedRaceNumber = true; break; }   //todo temp hier fehlt die Überprüfung ob auf Entrylist
                    }
                    if (adminRaceNumber == ushort.MaxValue) { adminRaceNumber = 0; break; }
                }
                AccEntry accEntry = new();
                accEntry.AddDriver(admin.User.SteamId, "Die", "Rennleitung", "SC" + adminNr.ToString());
                accEntrylist.AddEntry(accEntry.drivers, adminRaceNumber, true, true);
            }
            accEntrylist.WriteJson(path);
        }
    }
}
