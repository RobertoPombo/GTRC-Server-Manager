using System.Collections.ObjectModel;
using System.Windows;

using GTRC_Basics.Models;
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

        public void SetOnline()
        {
            //todo not yet implemented
        }

        public void SetOffline()
        {
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
