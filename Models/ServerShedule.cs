using System.Collections.ObjectModel;

using GTRC_Basics.Models;
using GTRC_WPF;

namespace GTRC_Server_Bot.Models
{
    public class ServerShedule : ObservableObject
    {
        private bool isOnline = false;

        public Server Server { get; set; } = new();

        public ObservableCollection<Session> Sessions { get; set; } = [];

        public bool IsOnline
        {
            get { return isOnline; }
            set
            {
                isOnline = value;
                RaisePropertyChanged();
            }
        }

        public void SetOnline()
        {
            //todo not yet implemented
        }

        public void SetOffline()
        {
            //todo not yet implemented
        }
    }
}
