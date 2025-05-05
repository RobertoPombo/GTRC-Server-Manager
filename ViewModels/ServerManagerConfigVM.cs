
using GTRC_Server_Bot.Configs;
using GTRC_WPF;

namespace GTRC_Server_Bot.ViewModels
{
    public class ServerManagerConfigVM : ObservableObject
    {
        private ServerManagerConfig selected = new();

        public ServerManagerConfigVM()
        {
            RestoreJsonCmd = new UICmd((o) => RestoreJson());
            SaveJsonCmd = new UICmd((o) => ServerManagerConfig.SaveJson(Selected));
            RestoreJson();
        }

        public ServerManagerConfig Selected
        {
            get { return selected; }
            set
            {
                selected = value;
                RaisePropertyChanged();
                PortUdpMin = selected.PortUdpMin;
                PortUdpMax = selected.PortUdpMax;
                PortTcpMin = selected.PortTcpMin;
                PortTcpMax = selected.PortTcpMax;
            }
        }

        public ushort PortUdpMin
        {
            get { return selected.PortUdpMin; }
            set { selected.PortUdpMin = value; RaisePropertyChanged(); }
        }

        public ushort PortUdpMax
        {
            get { return selected.PortUdpMax; }
            set { selected.PortUdpMax = value; RaisePropertyChanged(); }
        }

        public ushort PortTcpMin
        {
            get { return selected.PortTcpMin; }
            set { selected.PortTcpMin = value; RaisePropertyChanged(); }
        }

        public ushort PortTcpMax
        {
            get { return selected.PortTcpMax; }
            set { selected.PortTcpMax = value; RaisePropertyChanged(); }
        }

        public void RestoreJson()
        {
            Selected = ServerManagerConfig.LoadJson();
        }

        public UICmd RestoreJsonCmd { get; set; }
        public UICmd SaveJsonCmd { get; set; }
    }
}
