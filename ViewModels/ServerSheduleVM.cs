using System.Collections.ObjectModel;

using GTRC_Server_Bot.Models;
using GTRC_WPF;

namespace GTRC_Server_Bot.ViewModels
{
    public class ServerSheduleVM : ObservableObject
    {
        public ServerSheduleVM()
        {

        }

        public ObservableCollection<ServerShedule> List { get; set; } = [];
    }
}
