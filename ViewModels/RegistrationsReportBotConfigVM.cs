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
    public class RegistrationsReportBotConfigVM : ObservableObject
    {
        private static readonly Random random = new();

        private RegistrationsReportBotConfig selected = new();
        private RegistrationsReport registrationsReport = new();
        private StateBackgroundWorker state = StateBackgroundWorker.Off;
        private int timeRemaining = 0;
        private bool isRunning = false;
        private int waitQueueCount = 0;
        private BackgroundWorker backgroundWorker = new() { WorkerSupportsCancellation = true };

        public RegistrationsReportBotConfigVM()
        {
            RestoreJsonCmd = new UICmd((o) => RestoreJson());
            SaveJsonCmd = new UICmd((o) => RegistrationsReportBotConfig.SaveJson(Selected));
            SyncronizeRegistrationsCmd = new UICmd((o) => TriggerSyncronizeRegistrations());
            RestoreJson();
            GlobalWinValues.StateBackgroundWorkerColorsUpdated += RefreshStateColor;
        }

        public RegistrationsReportBotConfig Selected
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

        public void LoopSyncronizeRegistrations(object? sender, DoWorkEventArgs e)
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
                        TriggerSyncronizeRegistrations();
                        timeRemaining = (ushort)(IntervallMin * 60);
                    }
                }
            }
        }

        public void TriggerSyncronizeRegistrations()
        {
            new Thread(ThreadSyncronizeRegistrations).Start();
        }

        public async void ThreadSyncronizeRegistrations()
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
            Selected = RegistrationsReportBotConfig.LoadJson();
            SetState();
            if (!backgroundWorker.IsBusy)
            {
                backgroundWorker.DoWork += LoopSyncronizeRegistrations;
                backgroundWorker.RunWorkerAsync();
            }
        }

        public UICmd RestoreJsonCmd { get; set; }
        public UICmd SaveJsonCmd { get; set; }
        public UICmd SyncronizeRegistrationsCmd { get; set; }
    }
}
