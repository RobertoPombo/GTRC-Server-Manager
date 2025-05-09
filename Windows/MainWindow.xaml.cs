using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

using GTRC_Database_Client.Responses;
using GTRC_Database_Client;
using GTRC_WPF;
using GTRC_WPF_UserControls.ViewModels;
using GTRC_Server_Basics.Discord;

namespace GTRC_Server_Bot.Windows
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            GTRC_Basics.Scripts.CreateDirectories();
            GlobalWinValues.SetCultureInfo();
            UpdateThemeColors();
            DiscordCommands.DiscordBot.EnsureIsRunning();
            InitializeComponent();
            Width = 580;
            Height = 200;
            Left = 10;
            Top = 2 * 10 + 230;
            Closing += CloseWindow;
            DbApiConnectionConfigVM.ConfirmApiConnectionEstablished += UpdateThemeColors;
        }

        public void CloseWindow(object? sender, CancelEventArgs e) { }

        public void UpdateThemeColors() { _ = UpdateThemeColorsAsync(); }

        public async Task UpdateThemeColorsAsync()
        {
            DbApiListResponse<GTRC_Basics.Models.Color> response = await DbApi.DynCon.Color.GetAll();
            List<GTRC_Basics.Models.Color> colors = response.List;
            GlobalWinValues.UpdateWpfColors(this, colors);
        }
    }
}