using Newtonsoft.Json;
using System.IO;
using System.Text;

using GTRC_Basics;

namespace GTRC_Server_Bot.Configs
{
    public class ServerManagerConfig
    {
        public static readonly int MinSheduleLookAheadH = (int)Math.Round((double)(DbChangeDetectionConfig.MaxIntervallMin / 60), 0) + 1;
        public static readonly int MaxSheduleLookAheadH = 365 * 24;
        private static readonly string path = GlobalValues.ConfigDirectory + "config server manager.json";


        private int sheduleLookAheadH = MinSheduleLookAheadH;
        private ushort portUdpMin = ushort.MinValue;
        private ushort portUdpMax = ushort.MaxValue;
        private ushort portTcpMin = ushort.MinValue;
        private ushort portTcpMax = ushort.MaxValue;

        public int SheduleLookAheadH
        {
            get { return sheduleLookAheadH; }
            set { sheduleLookAheadH = Math.Min(MaxSheduleLookAheadH, Math.Max(MinSheduleLookAheadH, value)); }
        }

        public ushort PortUdpMin
        {
            get { return portUdpMin; }
            set { portUdpMin = Math.Min(portUdpMax, value); }
        }

        public ushort PortUdpMax
        {
            get { return portUdpMax; }
            set { portUdpMax = Math.Max(portUdpMin, value); }
        }

        public ushort PortTcpMin
        {
            get { return portTcpMin; }
            set { portTcpMin = Math.Min(portTcpMax, value); }
        }

        public ushort PortTcpMax
        {
            get { return portTcpMax; }
            set { portTcpMax = Math.Max(portTcpMin, value); }
        }

        public static ServerManagerConfig LoadJson()
        {
            ServerManagerConfig? config = null;
            if (!File.Exists(path)) { File.WriteAllText(path, JsonConvert.SerializeObject(new ServerManagerConfig(), Formatting.Indented), Encoding.Unicode); }
            try
            {
                config = JsonConvert.DeserializeObject<ServerManagerConfig>(File.ReadAllText(path, Encoding.Unicode));
                GlobalValues.CurrentLogText = "Reload server manager settings restored.";
            }
            catch { GlobalValues.CurrentLogText = "Restore reload server manager settings failed!"; }
            config ??= new ServerManagerConfig();
            return config;
        }

        public static void SaveJson(ServerManagerConfig config)
        {
            string text = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(path, text, Encoding.Unicode);
            GlobalValues.CurrentLogText = "Server manager settings saved.";
        }
    }
}
