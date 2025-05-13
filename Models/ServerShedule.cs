using System.Collections.ObjectModel;
using System.Windows;

using GTRC_Basics.Models;
using GTRC_Basics.Models.DTOs;
using GTRC_Database_Client;
using GTRC_WPF;

namespace GTRC_Server_Bot.Models
{
    public class ServerShedule : ObservableObject
    {
        private bool isOnline = false;
        private Visibility visibilityServerSessions = Visibility.Collapsed;

        public ServerShedule()
        {
            ShowHideSessionsCmd = new UICmd((o) => ShowHideSessions());
        }

        public Server Server { get; set; } = new();

        public ObservableCollection<ServerSession> ServerSessions { get; set; } = [];

        public bool IsOnline
        {
            get { return isOnline; }
            set
            {
                isOnline = value;
                RaisePropertyChanged();
            }
        }

        public Visibility VisibilityServerSessions { get { return visibilityServerSessions; } set { visibilityServerSessions = value; RaisePropertyChanged(); } }

        public async Task UpdateServerSessions()
        {
            foreach (ServerSession serverSession in ServerSessions)
            {
                Session firstSession = serverSession.Session;
                Session lastSession = serverSession.Session;
                List<Session> relatedSessions = (await DbApi.DynCon.Session.GetRelated(serverSession.Session.Id)).List;
                relatedSessions = GTRC_Basics.Scripts.SortByDate(relatedSessions);
                if (relatedSessions.Count > 0) { firstSession = relatedSessions[0]; lastSession = relatedSessions[^1]; }
                serverSession.StartDate = SessionFullDto.GetStartDate(firstSession);
                serverSession.EndDate = SessionFullDto.GetEndDate(lastSession);
            }
        }

        public void SetOnline()
        {
            //todo not yet implemented
        }

        public void SetOffline()
        {
            foreach (ServerSession serverSession in ServerSessions) { serverSession.StopBackgroundWorker(); }
            //todo not yet implemented
        }

        public void ShowHideSessions()
        {
            if (visibilityServerSessions == Visibility.Visible) { VisibilityServerSessions = Visibility.Collapsed; }
            else { VisibilityServerSessions = Visibility.Visible; }
        }

        public UICmd ShowHideSessionsCmd { get; set; }
    }
}
