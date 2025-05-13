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
                CountdownStartSec = (int)Math.Round((SessionFullDto.GetStartDate(session) - DateTime.UtcNow).TotalSeconds, 0);
                CountdownEndSec = (int)Math.Round((SessionFullDto.GetEndDate(session) - DateTime.UtcNow).TotalSeconds, 0);
                //while (!isActive) { Thread.Sleep(1000); }
                if (!backgroundWorker.IsBusy)
                {
                    backgroundWorker.DoWork += LoopRefreshCountdowns;
                    backgroundWorker.RunWorkerAsync();
                }
            }
        }

        public int CountdownStartSec { get; set; } = 0;
        public int CountdownEndSec { get; set; } = 0;

        public string CountdownStart { get { return GTRC_Basics.Scripts.TimeRemaining2StringPrecise(CountdownStartSec, delimiter: ":"); } }
        public string CountdownEnd { get { return GTRC_Basics.Scripts.TimeRemaining2StringPrecise(CountdownEndSec, delimiter: ":"); } }

        public void LoopRefreshCountdowns(object? sender, DoWorkEventArgs e)
        {
            while (isActive)
            {
                Thread.Sleep(1000);
                if (CountdownStartSec != 0)
                {
                    CountdownStartSec -= 1;
                    RaisePropertyChanged(nameof(CountdownStart));
                    if (CountdownStartSec <= 0)
                    {
                        CountdownStartSec = 0;
                        RaisePropertyChanged(nameof(CountdownStart));
                        // todo not yet implemented
                    }
                }
                if (CountdownEndSec != 0)
                {
                    CountdownEndSec -= 1;
                    RaisePropertyChanged(nameof(CountdownEnd));
                    if (CountdownEndSec <= 0)
                    {
                        CountdownEndSec = 0;
                        RaisePropertyChanged(nameof(CountdownEnd));
                        // todo not yet implemented
                    }
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
