﻿using System.IO;
using System.Net;
using System.Windows;

using GTRC_Basics;
using GTRC_Basics.Models;
using GTRC_Basics.Models.DTOs;
using GTRC_Basics.SimModels;
using GTRC_Database_Client;
using GTRC_Database_Client.Responses;
using GTRC_Server_Bot.Configs;
using GTRC_Server_Bot.Models;
using GTRC_Server_Bot.ViewModels;

namespace GTRC_Server_Bot.Scripts
{
    public class ServerUpdate
    {
        private static readonly string AdminRoleName = "Rennleitung";

        private static List<Session> allSessions = [];
        private static List<Session> upcomingSessions = [];

        public static async Task Update(ServerManagerConfig serverManagerConfig)
        {
            allSessions = (await DbApi.DynCon.Session.GetAll()).List;
            allSessions = GTRC_Basics.Scripts.SortByDate(allSessions);
            await SyncServerList();
            if (MainVM.Instance?.ServerSheduleVM is not null)
            {
                await UpdateResultsFolders();
                SyncSessionLists(serverManagerConfig);
                await UpdateServerShedules(serverManagerConfig);
                CheckForRestrictedPorts(serverManagerConfig);
                await UpdateServerFolders();
                UpdateServerSessions();
            }
        }

        private static async Task SyncServerList()
        {
            if (MainVM.Instance?.ServerSheduleVM is not null)
            {
                List<Server> listServers = (await DbApi.DynCon.Server.GetAll()).List;
                foreach (Server server in listServers)
                {
                    bool IsSynchronized = false;
                    foreach (ServerShedule serverShedule in MainVM.Instance.ServerSheduleVM.List)
                    {
                        if (server.PortUdp == serverShedule.Server.PortUdp || server.PortTcp == serverShedule.Server.PortTcp)
                        {
                            IsSynchronized = true;
                            serverShedule.Server = server;
                            for (int sessionNr = serverShedule.ServerSessions.Count - 1; sessionNr >= 0; sessionNr--)
                            {
                                Session session = serverShedule.ServerSessions[sessionNr].Session;
                                if (session.Event.Season.Series.SimId != serverShedule.Server.SimId && session.PreviousSessionId == 0 && SessionFullDto.GetStartDate(session) > DateTime.UtcNow)
                                {
                                    RemoveSessionFromShedule(serverShedule, sessionNr);
                                }
                            }
                            break;
                        }
                    }
                    if (!IsSynchronized) { Application.Current.Dispatcher.Invoke(() => { MainVM.Instance.ServerSheduleVM.List.Add(new() { Server = server }); }); }
                }
                for (int serverSheduleNr = MainVM.Instance.ServerSheduleVM.List.Count - 1; serverSheduleNr >= 0; serverSheduleNr--)
                {
                    ServerShedule serverShedule = MainVM.Instance.ServerSheduleVM.List[serverSheduleNr];
                    bool IsSynchronized = false;
                    foreach (Server server in listServers)
                    {
                        if (server.PortUdp == serverShedule.Server.PortUdp || server.PortTcp == serverShedule.Server.PortTcp)
                        {
                            IsSynchronized = true;
                            break;
                        }
                    }
                    if (!IsSynchronized)
                    {
                        serverShedule.SetOffline();
                        Application.Current.Dispatcher.Invoke(() => { MainVM.Instance.ServerSheduleVM.List.RemoveAt(serverSheduleNr); });
                    }
                }
            }
        }

        private static void RemoveSessionFromShedule(ServerShedule serverShedule, int sessionNr)
        {
            for (int nextSessionNr = serverShedule.ServerSessions.Count - 1; nextSessionNr >= 0; nextSessionNr--)
            {   // Folgesessions der entfernten Session ebenfalls entfernen
                if (serverShedule.ServerSessions[nextSessionNr].Session.PreviousSessionId == serverShedule.ServerSessions[sessionNr].Session.Id) { RemoveSessionFromShedule(serverShedule, nextSessionNr); }
            }
            serverShedule.ServerSessions[sessionNr].StopBackgroundWorker();
            Application.Current.Dispatcher.Invoke(() => { serverShedule.ServerSessions.RemoveAt(sessionNr); });   // Noch nicht gestartete Sessions aus "falscher" Sim entfernen
        }

        private static void SyncSessionLists(ServerManagerConfig serverManagerConfig)
        {
            foreach (Session session in allSessions)
            {
                if (SessionFullDto.GetEndDate(session) > DateTime.UtcNow)                                               // Session noch nicht vorbei
                {
                    if (SessionFullDto.GetStartDate(session) < DateTime.UtcNow) { upcomingSessions.Add(session); }      // Session ist bereits gestartet
                    else if (session.PreviousSessionId != GlobalValues.NoId || SessionFullDto.GetStartDate(session) < DateTime.UtcNow.AddHours(serverManagerConfig.SheduleLookAheadH)) { upcomingSessions.Add(session); }  // Session startet innerhalb des Look-Ahead-Zeitraums
                }
            }
        }

        private static async Task UpdateServerShedules(ServerManagerConfig serverManagerConfig)
        {
            if (MainVM.Instance?.ServerSheduleVM is not null)
            {
                foreach (Session session in upcomingSessions)
                {
                    if (!TryAppendToPreviousSession(session))
                    {
                        bool foundFreeServer = false;
                        bool isSheduled = false;
                        foreach (ServerShedule serverShedule in MainVM.Instance.ServerSheduleVM.List)
                        {
                            foundFreeServer = true;
                            if (serverShedule.Server.SimId != session.Event.Season.Series.SimId) { foundFreeServer = false; }
                            foreach (ServerSession serverSession in serverShedule.ServerSessions)
                            {
                                if (session.Id == serverSession.Session.Id) { isSheduled = true; break; }
                                else
                                {
                                    if (GTRC_Basics.Scripts.CheckDoTimeSpansOverlap(SessionFullDto.GetStartDate(session), SessionFullDto.GetStartDate(session),
                                        SessionFullDto.GetEndDate(serverSession.Session), SessionFullDto.GetEndDate(serverSession.Session)))
                                    {
                                        foundFreeServer = false;
                                    }
                                    if (!serverSession.Session.IsAllowedInterruption && SessionFullDto.GetStartDate(serverSession.Session) < SessionFullDto.GetStartDate(session)) { foundFreeServer = false; }
                                }
                            }
                            if (isSheduled) { break; }
                            if (foundFreeServer)
                            {
                                Application.Current.Dispatcher.Invoke(() => { serverShedule.ServerSessions.Add(new(session)); });
                                break;
                            }
                        }
                        if (!isSheduled && !foundFreeServer) { await CreateNewServer(session, serverManagerConfig); }
                    }
                }
            }
        }

        private static bool TryAppendToPreviousSession(Session session)
        {
            if (session.PreviousSessionId > GlobalValues.NoId)
            {
                foreach (ServerShedule serverShedule in MainVM.Instance?.ServerSheduleVM?.List ?? [])
                {
                    foreach (ServerSession serverSession in serverShedule.ServerSessions)
                    {
                        if (session.PreviousSessionId == serverSession.Session.Id)
                        {
                            bool isSheduled = false;
                            foreach (ServerShedule _serverShedule in MainVM.Instance?.ServerSheduleVM?.List ?? [])
                            {
                                foreach (ServerSession _serverSession in _serverShedule.ServerSessions)
                                {
                                    if (session.Id == _serverSession.Session.Id) { isSheduled = true; }
                                }
                            }
                            if (!isSheduled) { Application.Current.Dispatcher.Invoke(() => { serverShedule.ServerSessions.Add(new(session)); }); }
                            return true;
                        }
                    }
                }
                return true;
            }
            return false;
        }

        private static async Task CreateNewServer(Session session, ServerManagerConfig serverManagerConfig)
        {
            Server server = new() { PortUdp = serverManagerConfig.PortUdpMin, PortTcp = serverManagerConfig.PortTcpMin, SimId = session.Event.Season.Series.SimId };
            ServerAddDto serverAddDto = new();
            serverAddDto.Model2Dto(server);
            DbApiObjectResponse<Server> respAddServer = await DbApi.DynCon.Server.Add(new() { Dto = serverAddDto });
            if (respAddServer.Status == HttpStatusCode.AlreadyReported)
            {
                serverAddDto.Model2Dto(respAddServer.Object);
                respAddServer = await DbApi.DynCon.Server.Add(new() { Dto = serverAddDto });
            }
            if (respAddServer.Status == HttpStatusCode.OK)
            {
                ServerShedule serverShedule = new() { Server = respAddServer.Object };
                Application.Current.Dispatcher.Invoke(() => { serverShedule.ServerSessions.Add(new(session)); });
                Application.Current.Dispatcher.Invoke(() => { MainVM.Instance?.ServerSheduleVM?.List.Add(serverShedule); });
            }
            else
            {
                GlobalValues.CurrentLogText = "Server konnte nicht erstellt werden! (Udp-Port: " + respAddServer.Object.PortUdp.ToString() + ", Tcp-Port: " + respAddServer.Object.PortTcp.ToString() + ")";
            }
        }

        private static void CheckForRestrictedPorts(ServerManagerConfig serverManagerConfig)
        {
            if (MainVM.Instance?.ServerSheduleVM is not null)
            {
                string delimiter = ",";
                List<ushort> restrictedUdpPorts = [];
                List<ushort> restrictedTcpPorts = [];
                foreach (ServerShedule serverShedule in MainVM.Instance.ServerSheduleVM.List)
                {
                    if (serverShedule.Server.PortUdp < serverManagerConfig.PortUdpMin || serverShedule.Server.PortUdp > serverManagerConfig.PortUdpMax)
                    {
                        restrictedUdpPorts.Add(serverShedule.Server.PortUdp);
                    }
                    if (serverShedule.Server.PortTcp < serverManagerConfig.PortTcpMin || serverShedule.Server.PortTcp > serverManagerConfig.PortTcpMax)
                    {
                        restrictedTcpPorts.Add(serverShedule.Server.PortTcp);
                    }
                }
                if (restrictedUdpPorts.Count > 0 || restrictedTcpPorts.Count > 0)
                {
                    string logText = string.Empty;
                    if (restrictedUdpPorts.Count > 0)
                    {
                        logText += "Udp-Port";
                        if (restrictedUdpPorts.Count > 1) { logText += "s"; }
                        foreach (ushort port in restrictedUdpPorts) { logText += " " + port.ToString() + delimiter; }
                    }
                    if (restrictedUdpPorts.Count > 0 && restrictedTcpPorts.Count > 0) { logText = logText[..^delimiter.Length] + " und "; }
                    if (restrictedTcpPorts.Count > 0)
                    {
                        logText += "Tcp-Port";
                        if (restrictedTcpPorts.Count > 1) { logText += "s"; }
                        foreach (ushort port in restrictedTcpPorts) { logText += " " + port.ToString() + delimiter; }
                    }
                    logText = logText[..^delimiter.Length];
                    if (restrictedUdpPorts.Count + restrictedTcpPorts.Count < 2) { logText += " muss"; }
                    else { logText += " müssen"; }
                    logText += " freigegeben werden!";
                    GlobalValues.CurrentLogText = logText;
                }
            }
        }

        private static async Task UpdateServerFolders()
        {
            List<Server> listServers = (await DbApi.DynCon.Server.GetAll()).List;
            DirectoryInfo serverDirectoryInfo = new(GlobalValues.ServerDirectory);
            DirectoryInfo[] serverFolders = serverDirectoryInfo.GetDirectories();

            //Existierende Ordner ggf. umbenennen
            foreach (DirectoryInfo serverFolder in serverFolders)
            {
                bool doesExist = false;
                foreach (Server server in listServers)
                {
                    if (server.ToString() == serverFolder.Name) { doesExist = true; break; }
                }
                if (!doesExist) { DeleteFolder(GlobalValues.ServerDirectory, serverFolder); }
            }

            //Fehlende Ordner erstellen
            foreach (Server server in listServers)
            {
                string path = GlobalValues.ServerDirectory + server.ToString() + "\\";
                if (!Directory.Exists(path)) { try { Directory.CreateDirectory(path); } catch { } }

                //Ordnerstruktur prüfen
                if (server.Sim.Name == "Assetto Corsa Competizione")
                {
                    string pathCfg = path + "cfg\\";
                    string pathResults = path + "results\\";
                    string pathLog = path + "log\\";
                    string pathCfgCurrent = pathCfg + "current\\";
                    if (!Directory.Exists(pathCfg)) { try { Directory.CreateDirectory(pathCfg); } catch { } }
                    if (!Directory.Exists(pathResults)) { try { Directory.CreateDirectory(pathResults); } catch { } }
                    if (!Directory.Exists(pathLog)) { try { Directory.CreateDirectory(pathLog); } catch { } }
                    if (!Directory.Exists(pathCfgCurrent)) { try { Directory.CreateDirectory(pathCfgCurrent); } catch { } }
                }
            }
        }

        private static async Task UpdateResultsFolders()
        {
            //Existierende Ordner ggf. umbenennen
            DirectoryInfo seriesDirectoryInfo = new(GlobalValues.ResultsDirectory);
            DirectoryInfo[] seriesFolders = seriesDirectoryInfo.GetDirectories();
            List<Series> listSeries = (await DbApi.DynCon.Series.GetAll()).List;
            foreach (DirectoryInfo seriesFolder in seriesFolders)
            {
                bool doesExistSeries = false;
                foreach (Series series in listSeries)
                {
                    if (series.Name == seriesFolder.Name)
                    {
                        DirectoryInfo seasonDirectoryInfo = new(seriesFolder.FullName);
                        DirectoryInfo[] seasonFolders = seasonDirectoryInfo.GetDirectories();
                        List<Season> listSeasons = (await DbApi.DynCon.Season.GetChildObjects(typeof(Series), series.Id)).List;
                        foreach (DirectoryInfo seasonFolder in seasonFolders)
                        {
                            bool doesExistSeason = false;
                            foreach (Season season in listSeasons)
                            {
                                if (season.Name == seasonFolder.Name)
                                {
                                    DirectoryInfo eventDirectoryInfo = new(seasonFolder.FullName);
                                    DirectoryInfo[] eventFolders = eventDirectoryInfo.GetDirectories();
                                    List<Event> listEvents = (await DbApi.DynCon.Event.GetChildObjects(typeof(Season), season.Id)).List;
                                    foreach (DirectoryInfo eventFolder in eventFolders)
                                    {
                                        bool doesExistEvent = false;
                                        foreach (Event _event in listEvents)
                                        {
                                            if (_event.Name == eventFolder.Name)
                                            {
                                                DirectoryInfo sessionDirectoryInfo = new(eventFolder.FullName);
                                                DirectoryInfo[] sessionFolders = sessionDirectoryInfo.GetDirectories();
                                                List<Session> listSessions = (await DbApi.DynCon.Session.GetChildObjects(typeof(Event), _event.Id)).List;
                                                listSessions = GTRC_Basics.Scripts.SortByDate(listSessions);
                                                foreach (DirectoryInfo sessionFolder in sessionFolders)
                                                {
                                                    bool doesExistSession = false;
                                                    Dictionary<SessionType, int> dictEventSessionTypeCount = [];
                                                    foreach (Session session in listSessions)
                                                    {
                                                        if (dictEventSessionTypeCount.ContainsKey(session.SessionType)) { dictEventSessionTypeCount[session.SessionType] += 1; }
                                                        else { dictEventSessionTypeCount[session.SessionType] = 1; }
                                                        string sessionFolderName = session.SessionType.ToString() + " #" + dictEventSessionTypeCount[session.SessionType].ToString() + " - " + session.ServerName + " (" +
                                                        GTRC_Basics.Scripts.Date2String(SessionFullDto.GetStartDate(session), "DD-MM-YY hh-mm") + " - " + GTRC_Basics.Scripts.Date2String(SessionFullDto.GetEndDate(session), "DD-MM-YY hh-mm") + ")";
                                                        sessionFolderName = GTRC_Basics.Scripts.PathRemoveBlacklistedChars(sessionFolderName).Replace("  ", " ");
                                                        if (sessionFolderName == sessionFolder.Name)
                                                        {
                                                            doesExistSession = true;
                                                            break;
                                                        }
                                                    }
                                                    if (!doesExistSession) { DeleteFolder(GlobalValues.ResultsDirectory, sessionFolder); }
                                                }
                                                doesExistEvent = true;
                                                break;
                                            }
                                        }
                                        if (!doesExistEvent) { DeleteFolder(GlobalValues.ResultsDirectory, eventFolder); }
                                    }
                                    doesExistSeason = true;
                                    break;
                                }
                            }
                            if (!doesExistSeason) { DeleteFolder(GlobalValues.ResultsDirectory, seasonFolder); }
                        }
                        doesExistSeries = true;
                        break;
                    }
                }
                if (!doesExistSeries) { DeleteFolder(GlobalValues.ResultsDirectory, seriesFolder); }
            }

            //Fehlende Ordner erstellen
            List<string> listPaths = [];
            Dictionary<(int, SessionType), int> dictSessionTypeCount = [];
            foreach (Session session in allSessions)
            {
                if (dictSessionTypeCount.ContainsKey((session.EventId, session.SessionType))) { dictSessionTypeCount[(session.EventId, session.SessionType)] += 1; }
                else { dictSessionTypeCount[(session.EventId, session.SessionType)] = 1; }
                string path = session.Event.Season.Series.Name + "\\" + session.Event.Season.Name + "\\" + session.Event.Name + "\\" + session.SessionType.ToString() + " #" +
                    dictSessionTypeCount[(session.EventId, session.SessionType)].ToString() + " - " + session.ServerName + " (" +
                    GTRC_Basics.Scripts.Date2String(SessionFullDto.GetStartDate(session), "DD-MM-YY hh-mm") + " - " + GTRC_Basics.Scripts.Date2String(SessionFullDto.GetEndDate(session), "DD-MM-YY hh-mm") + ")";
                path = GTRC_Basics.Scripts.PathRemoveBlacklistedChars(path);
                path = GlobalValues.ResultsDirectory + path.Replace("  ", " ");
                listPaths.Add(path);
                if (!Directory.Exists(path)) { try { Directory.CreateDirectory(path); } catch { } }
            }
        }

        public static void DeleteFolder(string basePath, DirectoryInfo folder)
        {
            string deletePath = basePath + "#delete#";
            if (!Directory.Exists(deletePath)) { try { Directory.CreateDirectory(deletePath); } catch { } }
            string newPath = deletePath + folder.Name;
            if (basePath.Length < folder.FullName.Length) { newPath = deletePath + "\\" + folder.FullName[basePath.Length..]; }
            if (folder.FullName != newPath && (deletePath.Length > folder.FullName.Length || folder.FullName[..deletePath.Length] != deletePath))
            {
                if (newPath.Length > folder.Name.Length && !Directory.Exists(newPath[..^folder.Name.Length])) { try { Directory.CreateDirectory(newPath[..^folder.Name.Length]); } catch { } }
                try { Directory.Move(folder.FullName, newPath); }
                catch { }
            }
        }

        private static void UpdateServerSessions()
        {
            foreach (ServerShedule serverShedule in MainVM.Instance?.ServerSheduleVM?.List ?? [])
            {
                _ = serverShedule.UpdateServerSessions();
            }
        }

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
