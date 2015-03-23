using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace SyncTrayzor.Services
{
    public class MemoryUsageLogger
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly string[] sizes = { "B", "KB", "MB", "GB" };
        private static readonly TimeSpan pollInterval = TimeSpan.FromMinutes(30);

        private readonly Timer timer;
        private readonly Process process;

        public bool Enabled
        {
            get { return this.timer.Enabled; }
            set { this.timer.Enabled = value; }
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
                logger.Debug("Working Set: {0}. Private Memory Size: {1}. GC Total Memory: {2}",
                    this.BytesToHuman(this.process.WorkingSet64), this.BytesToHuman(this.process.PrivateMemorySize64),
                    this.BytesToHuman(GC.GetTotalMemory(false)));
            };
        }

        private string BytesToHuman(long bytes)
        {
            // http://stackoverflow.com/a/281679/1086121
            int order = 0;
            while (bytes >= 1024 && order + 1 < sizes.Length)
            {
                order++;
                bytes = bytes / 1024;
            }
            return String.Format("{0:0.#}{1}", bytes, sizes[order]);
        }
    }
}
