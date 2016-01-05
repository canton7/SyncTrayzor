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

        List<string> ConflictedFiles { get; }
    }

    public class AlertsManager : IAlertsManager
    {
        private readonly IConflictFileWatcher conflictFileWatcher;
        private readonly SynchronizedEventDispatcher eventDispatcher;

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
