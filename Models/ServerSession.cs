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

        public int CountdownStartSec { get; set; } = 0;
        public int CountdownEndSec { get; set; } = 0;

        public string CountdownStart { get { return GTRC_Basics.Scripts.TimeRemaining2StringPrecise(CountdownStartSec); } }
        public string CountdownEnd { get { return GTRC_Basics.Scripts.TimeRemaining2StringPrecise(CountdownEndSec); } }
    }
}
