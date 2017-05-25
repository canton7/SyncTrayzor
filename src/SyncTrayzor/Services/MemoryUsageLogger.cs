using NLog;
using SyncTrayzor.Utils;
using System;
using System.Diagnostics;
using System.Timers;

namespace SyncTrayzor.Services
{
    public class MemoryUsageLogger
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly TimeSpan pollInterval = TimeSpan.FromMinutes(5);

        private readonly Timer timer;
        private readonly Process process;

        public bool Enabled
        {
            get => this.timer.Enabled;
            set => this.timer.Enabled = value;
        }

        public MemoryUsageLogger()
        {
            this.process = Process.GetCurrentProcess();

            this.timer = new Timer()
            {
                AutoReset = true,
                Interval = pollInterval.TotalMilliseconds,
            };
            this.timer.Elapsed += (o, e) =>
            {
                logger.Info("Working Set: {0}. Private Memory Size: {1}. GC Total Memory: {2}",
                    FormatUtils.BytesToHuman(this.process.WorkingSet64), FormatUtils.BytesToHuman(this.process.PrivateMemorySize64),
                    FormatUtils.BytesToHuman(GC.GetTotalMemory(true)));
            };
        }
    }
}
