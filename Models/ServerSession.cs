using System.Windows;

using GTRC_Basics.Models;
using GTRC_WPF;

namespace GTRC_Server_Bot.Models
{
    public class ServerSession : ObservableObject
    {
        public ServerSession()
        {

        }

        public Session Session { get; set; } = new();
    }
}
