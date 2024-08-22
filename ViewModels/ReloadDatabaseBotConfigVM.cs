using System.ComponentModel;
using System.Windows.Media;

using GTRC_Basics.Models;
using GTRC_Database_Client;
using GTRC_Database_Client.Responses;
using GTRC_Server_Bot.Configs;
using GTRC_Server_Bot.Scripts;
using GTRC_WPF;

namespace GTRC_Server_Bot.ViewModels
{
    public class ReloadDatabaseBotConfigVM : ObservableObject
    {
        private static readonly Random random = new();

        private ReloadDatabaseBotConfig selected = new();
        private RegistrationsReport registrationsReport = new();
        private StateBackgroundWorker state = StateBackgroundWorker.Off;
        private int timeRemaining = 0;
        private bool isRunning = false;
        private int waitQueueCount = 0;
        private BackgroundWorker backgroundWorker = new() { WorkerSupportsCancellation = true };

        public ReloadDatabaseBotConfigVM()
        {
            RestoreJsonCmd = new UICmd((o) => RestoreJson());
            SaveJsonCmd = new UICmd((o) => ReloadDatabaseBotConfig.SaveJson(Selected));
            ReloadDatabaseCmd = new UICmd((o) => TriggerReloadDatabase());
            RestoreJson();
            GlobalWinValues.StateBackgroundWorkerColorsUpdated += RefreshStateColor;
        }

        public ReloadDatabaseBotConfig Selected
        {
            get { return selected; }
            set
            {
                selected = value;
                RaisePropertyChanged();
                IntervallMin = selected.IntervallMin;
                IsActive = selected.IsActive;
            }
        }

        public int IntervallMin
        {
            get { return selected.IntervallMin; }
            set
            {
                selected.IntervallMin = value;
                timeRemaining = (ushort)(IntervallMin * 60);
                TimeRemaining = string.Empty;
                RaisePropertyChanged();
            }
        }

        public bool IsActive
        {
            get { return selected.IsActive; }
            set
            {
                selected.IsActive = value;
                RaisePropertyChanged();
                if (IsActive && State == StateBackgroundWorker.Off) { State = StateBackgroundWorker.On; }
                else if (!IsActive && State == StateBackgroundWorker.On) { State = StateBackgroundWorker.Off; }
                timeRemaining = (ushort)(IntervallMin * 60);
                TimeRemaining = string.Empty;
            }
        }

        public StateBackgroundWorker State
        {
            get { return state; }
            set { state = value; RaisePropertyChanged(nameof(StateColor)); }
        }

        public Brush StateColor
        {
            get { return GlobalWinValues.ColorsStateBackgroundWorker[State]; }
        }

        public bool IsRunning
        {
            get { return isRunning; }
            set { isRunning = value; SetState(); }
        }

        public int WaitQueueCount
        {
            get { return waitQueueCount; }
            set { if (value >= 0) { waitQueueCount = value; SetState(); } }
        }

        public string TimeRemaining
        {
            get
            {
                if (timeRemaining > 2 * 60 * 60) { return ((int)Math.Ceiling((double)timeRemaining / (60 * 60))).ToString() + " h"; }
                else if (timeRemaining > 2 * 60) { return ((int)Math.Ceiling((double)timeRemaining / 60)).ToString() + " min"; }
                else { return timeRemaining.ToString() + " sec"; }
            }
            set { RaisePropertyChanged(); }
        }

        public void LoopReloadDatabase(object? sender, DoWorkEventArgs e)
        {
            while (true)
            {
                Thread.Sleep(1000);
                if (IsActive)
                {
                    timeRemaining -= 1;
                    TimeRemaining = string.Empty;
                    if (timeRemaining == 0)
                    {
                        TriggerReloadDatabase();
                        timeRemaining = (ushort)(IntervallMin * 60);
                    }
                }
            }
        }

        public void TriggerReloadDatabase()
        {
            new Thread(ThreadReloadDatabase).Start();
        }

        public async void ThreadReloadDatabase()
        {
            WaitQueueCount++;
            while (IsRunning) { Thread.Sleep(200 + random.Next(100)); }
            IsRunning = true;
            WaitQueueCount--;
            DbApiListResponse<Season> respSea = await DbApi.DynCon.Season.GetAll();
            foreach (Season season in respSea.List) { await registrationsReport.UpdateListEntries(season); }
            IsRunning = false;
        }

        public void SetState()
        {
            if (IsRunning)
            {
                if (WaitQueueCount > 0) { State = StateBackgroundWorker.RunWait; }
                else { State = StateBackgroundWorker.Run; }
            }
            else
            {
                if (WaitQueueCount > 0) { State = StateBackgroundWorker.Wait; }
                else { if (IsActive) { State = StateBackgroundWorker.On; } else { State = StateBackgroundWorker.Off; } }
            }
        }

        public void RefreshStateColor() { RaisePropertyChanged(nameof(StateColor)); }

        public void RestoreJson()
        {
            Selected = ReloadDatabaseBotConfig.LoadJson();
            SetState();
            if (!backgroundWorker.IsBusy)
            {
                backgroundWorker.DoWork += LoopReloadDatabase;
                backgroundWorker.RunWorkerAsync();
            }
        }

        public UICmd RestoreJsonCmd { get; set; }
        public UICmd SaveJsonCmd { get; set; }
        public UICmd ReloadDatabaseCmd { get; set; }
    }
}
