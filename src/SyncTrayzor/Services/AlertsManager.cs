using SyncTrayzor.Services;
using SyncTrayzor.Services.Conflicts;
using SyncTrayzor.SyncThing;
using SyncTrayzor.SyncThing.ApiClient;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SyncTrayzor.SyncThing.TransferHistory;

namespace SyncTrayzor.Services
{
    public interface IAlertsManager : IDisposable
    {
        event EventHandler AlertsStateChanged;
        bool AnyAlerts { get; }

        IReadOnlyList<string> ConflictedFiles { get; }

        IReadOnlyList<string> FoldersWithFailedTransferFiles { get; }
    }

    public class AlertsManager : IAlertsManager
    {
        private readonly ISyncThingManager syncThingManager;
        private readonly IConflictFileWatcher conflictFileWatcher;
        private readonly SynchronizedEventDispatcher eventDispatcher;

        public bool AnyAlerts => this.ConflictedFiles.Count > 0 || this.FoldersWithFailedTransferFiles.Count > 0;

        public IReadOnlyList<string> ConflictedFiles { get; private set; } = new List<string>().AsReadOnly();

        public IReadOnlyList<string> FoldersWithFailedTransferFiles { get; private set; } = new List<string>().AsReadOnly();

        public event EventHandler AlertsStateChanged;

        public AlertsManager(ISyncThingManager syncThingManager, IConflictFileWatcher conflictFileWatcher)
        {
            this.syncThingManager = syncThingManager;
            this.conflictFileWatcher = conflictFileWatcher;

            this.syncThingManager.TransferHistory.TransferCompleted += this.TransferCompleted;

            this.conflictFileWatcher.ConflictedFilesChanged += this.ConflictFilesChanged;

            this.eventDispatcher = new SynchronizedEventDispatcher(this);
        }

        private void TransferCompleted(object sender, FileTransferChangedEventArgs e)
        {
            var oldFoldersWithOutOfSyncFiles = new HashSet<string>(this.FoldersWithFailedTransferFiles);
            var newFoldersWithOutOfSyncFiles = new HashSet<string>(this.syncThingManager.TransferHistory.FailingTransfers.Select(x => x.FolderId));

            if (oldFoldersWithOutOfSyncFiles.SetEquals(newFoldersWithOutOfSyncFiles))
                return;

            this.FoldersWithFailedTransferFiles = newFoldersWithOutOfSyncFiles.ToList().AsReadOnly();

            this.eventDispatcher.Raise(this.AlertsStateChanged);
        }

        private void ConflictFilesChanged(object sender, EventArgs e)
        {
            this.ConflictedFiles = this.conflictFileWatcher.ConflictedFiles.AsReadOnly();
            this.eventDispatcher.Raise(this.AlertsStateChanged);
        }

        public void Dispose()
        {
            this.syncThingManager.TransferHistory.TransferCompleted -= this.TransferCompleted;
            this.conflictFileWatcher.ConflictedFilesChanged -= this.ConflictFilesChanged;
        }
    }
}
