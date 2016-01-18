using SyncTrayzor.Services.Conflicts;
using SyncTrayzor.Syncthing;
using SyncTrayzor.Syncthing.Folders;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SyncTrayzor.Services
{
    public interface IAlertsManager : IDisposable
    {
        event EventHandler AlertsStateChanged;
        bool AnyAlerts { get; }

        bool EnableFailedTransferAlerts { get; set; }
        bool EnableConflictedFileAlerts { get; set; }

        IReadOnlyList<string> ConflictedFiles { get; }

        IReadOnlyList<string> FoldersWithFailedTransferFiles { get; }
    }

    public class AlertsManager : IAlertsManager
    {
        private readonly ISyncthingManager syncthingManager;
        private readonly IConflictFileWatcher conflictFileWatcher;
        private readonly SynchronizedEventDispatcher eventDispatcher;

        public bool AnyAlerts => this.ConflictedFiles.Count > 0 || this.FoldersWithFailedTransferFiles.Count > 0;


        private IReadOnlyList<string> _conflictedFiles = EmptyReadOnlyList<string>.Instance;
        public IReadOnlyList<string> ConflictedFiles
        {
            get { return this._enableConflictedFileAlerts ? this._conflictedFiles : EmptyReadOnlyList<string>.Instance; }
        }

        private IReadOnlyList<string> _foldersWithFailedTransferFiles = EmptyReadOnlyList<string>.Instance;
        public IReadOnlyList<string> FoldersWithFailedTransferFiles
        {
            get { return this._enableFailedTransferAlerts ? this._foldersWithFailedTransferFiles : EmptyReadOnlyList<string>.Instance; }
        }

        public event EventHandler AlertsStateChanged;

        private bool _enableFailedTransferAlerts;
        public bool EnableFailedTransferAlerts
        {
            get { return this._enableFailedTransferAlerts; }
            set
            {
                if (this._enableFailedTransferAlerts == value)
                    return;
                this._enableFailedTransferAlerts = value;
                this.OnAlertsStateChanged();
            }
        }

        private bool _enableConflictedFileAlerts;
        public bool EnableConflictedFileAlerts
        {
            get { return this._enableConflictedFileAlerts; }
            set
            {
                if (this._enableConflictedFileAlerts == value)
                    return;
                this._enableConflictedFileAlerts = value;
                this.OnAlertsStateChanged();
            }
        }

        public AlertsManager(ISyncthingManager syncthingManager, IConflictFileWatcher conflictFileWatcher)
        {
            this.syncthingManager = syncthingManager;
            this.conflictFileWatcher = conflictFileWatcher;
            this.eventDispatcher = new SynchronizedEventDispatcher(this);

            this.syncthingManager.Folders.FolderErrorsChanged += this.FolderErrorsChanged;

            this.conflictFileWatcher.ConflictedFilesChanged += this.ConflictFilesChanged;
        }

        private void OnAlertsStateChanged()
        {
            this.eventDispatcher.Raise(this.AlertsStateChanged);
        }

        private void FolderErrorsChanged(object sender, FolderErrorsChangedEventArgs e)
        {
            var folders = this.syncthingManager.Folders.FetchAll();
            this._foldersWithFailedTransferFiles = folders.Where(x => x.FolderErrors.Any()).Select(x => x.FolderId).ToList().AsReadOnly();
 
            this.OnAlertsStateChanged();
        }

        private void ConflictFilesChanged(object sender, EventArgs e)
        {
            this._conflictedFiles = this.conflictFileWatcher.ConflictedFiles.ToList().AsReadOnly();

            this.OnAlertsStateChanged();
        }

        public void Dispose()
        {
            this.syncthingManager.Folders.FolderErrorsChanged -= this.FolderErrorsChanged;
            this.conflictFileWatcher.ConflictedFilesChanged -= this.ConflictFilesChanged;
        }

        private static class EmptyReadOnlyList<T>
        {
            public static readonly IReadOnlyList<T> Instance = new List<T>().AsReadOnly();
        }
    }
}
