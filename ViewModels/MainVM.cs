using GTRC_WPF_UserControls.ViewModels;

namespace GTRC_Server_Bot.ViewModels
{
    public class MainVM : GTRC_WPF.ViewModels.MainVM
    {
        public static MainVM? Instance;

        private DbApiConnectionConfigVM? dbApiConnectionConfigVM;
        private DiscordBotConfigVM? discordBotConfigVM;
        private RegistrationsReportBotConfigVM? registrationsReportBotConfigVM;

        public MainVM()
        {
            Instance = this;
            DbApiConnectionConfigVM = new DbApiConnectionConfigVM();
            DiscordBotConfigVM = new DiscordBotConfigVM();
            RegistrationsReportBotConfigVM = new RegistrationsReportBotConfigVM();
        }

        public DbApiConnectionConfigVM? DbApiConnectionConfigVM { get { return dbApiConnectionConfigVM; } set { dbApiConnectionConfigVM = value; RaisePropertyChanged(); } }

        public DiscordBotConfigVM? DiscordBotConfigVM { get { return discordBotConfigVM; } set { discordBotConfigVM = value; RaisePropertyChanged(); } }

        public RegistrationsReportBotConfigVM? RegistrationsReportBotConfigVM { get { return registrationsReportBotConfigVM; } set { registrationsReportBotConfigVM = value; RaisePropertyChanged(); } }
    }
}
