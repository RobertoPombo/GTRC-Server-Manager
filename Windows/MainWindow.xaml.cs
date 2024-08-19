using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;

using GTRC_Basics;
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
            Width = GlobalWinValues.screenWidth * 0.32;
            Height = GlobalWinValues.screenHeight * 0.135;
            Left = GlobalWinValues.screenWidth * 0.005;
            Top = GlobalWinValues.screenWidth * 0.01 + GlobalWinValues.screenHeight * 0.07;
            Closing += CloseWindow;
            DbApiConnectionConfigVM.ConfirmApiConnectionEstablished += UpdateThemeColors;
        }

        public void CloseWindow(object? sender, CancelEventArgs e) { }

        public void UpdateThemeColors() { _ = UpdateThemeColorsAsync(); }

        public async Task UpdateThemeColorsAsync()
        {
            DbApiListResponse<GTRC_Basics.Models.Color> response = await DbApi.DynCon.Color.GetAll();
            List<GTRC_Basics.Models.Color> colors = response.List;
            for (int colorNr = 0; colorNr < colors.Count; colorNr++)
            {
                SolidColorBrush _color = new(Color.FromArgb(colors[colorNr].Alpha, colors[colorNr].Red, colors[colorNr].Green, colors[colorNr].Blue));
                if (colorNr < WpfColors.List.Count) { WpfColors.List[colorNr] = _color; }
                else { WpfColors.List.Add(_color); }
            }
            GlobalWinValues.UpdateWpfColors(this);
        }
    }
}