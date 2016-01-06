using SyncTrayzor.Services.Conflicts;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Services
{
    public interface IAlertsManager
    {
        event EventHandler AlertsStateChanged;
        bool AnyAlerts { get; }

        List<string> ConflictedFiles { get; }
    }

    public class AlertsManager : IAlertsManager
    {
        private readonly IConflictFileWatcher conflictFileWatcher;
        private readonly SynchronizedEventDispatcher eventDispatcher;

        public bool AnyAlerts => this.ConflictedFiles.Count > 0;

        public List<string> ConflictedFiles => this.conflictFileWatcher.ConflictedFiles;

        public event EventHandler AlertsStateChanged;

        public AlertsManager(IConflictFileWatcher conflictFileWatcher)
        {
            this.conflictFileWatcher = conflictFileWatcher;
            this.conflictFileWatcher.ConflictedFilesChanged += this.ConflictFilesChanged;

            this.eventDispatcher = new SynchronizedEventDispatcher(this);
        }

        private void ConflictFilesChanged(object sender, EventArgs e)
        {
            this.eventDispatcher.Raise(this.AlertsStateChanged);
        }
    }
}
