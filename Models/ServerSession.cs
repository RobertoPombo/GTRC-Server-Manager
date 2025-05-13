using System.ComponentModel;

using GTRC_Basics.Models;
using GTRC_Basics.Models.DTOs;
using GTRC_WPF;

namespace GTRC_Server_Bot.Models
{
    public class ServerSession : ObservableObject
    {
        private BackgroundWorker backgroundWorker = new() { WorkerSupportsCancellation = true };
        private bool isActive = true;
        private Session session = new();
        private string countdownStart = string.Empty;
        private string countdownEnd = string.Empty;

        public ServerSession(Session _session)
        {
            Session = _session;
        }

        public Session Session
        {
            get { return session; }
            set
            {
                session = value;
                //while (!isActive) { Thread.Sleep(1000); }
                if (!backgroundWorker.IsBusy)
                {
                    backgroundWorker.DoWork += LoopRefreshCountdowns;
                    backgroundWorker.RunWorkerAsync();
                }
            }
        }

        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; } = DateTime.UtcNow;

        public int CountdownStartSec { get { return (int)Math.Max(Math.Round((StartDate - DateTime.UtcNow).TotalSeconds, 0), 0); } }
        public int CountdownEndSec { get { return (int)Math.Max(Math.Round((EndDate - DateTime.UtcNow).TotalSeconds, 0), 0); } }

        public string CountdownStart { get { return countdownStart; } set { countdownStart = value; RaisePropertyChanged(); } }
        public string CountdownEnd { get { return countdownEnd; } set { countdownEnd = value; RaisePropertyChanged(); } }

        public void LoopRefreshCountdowns(object? sender, DoWorkEventArgs e)
        {
            while (isActive)
            {
                Thread.Sleep(500);
                CountdownStart = GTRC_Basics.Scripts.TimeRemaining2StringPrecise(CountdownStartSec, delimiter: ":");
                CountdownEnd = GTRC_Basics.Scripts.TimeRemaining2StringPrecise(CountdownEndSec, delimiter: ":");
                RaisePropertyChanged(nameof(CountdownEnd));
                if (CountdownStartSec == 0)
                {
                    // todo not yet implemented
                }
                if (CountdownEndSec == 0)
                {
                    // todo not yet implemented
                }
            }
            isActive = true;
        }
        
        public void StopBackgroundWorker()
        {
            isActive = false;
        }
    }
}
