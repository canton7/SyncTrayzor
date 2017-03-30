using SyncTrayzor.Services.Conflicts;
using SyncTrayzor.Services.Metering;
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
        bool AnyWarnings { get; }

        bool EnableFailedTransferAlerts { get; set; }
        bool EnableConflictedFileAlerts { get; set; }

        IReadOnlyList<string> ConflictedFiles { get; }

        IReadOnlyList<string> FoldersWithFailedTransferFiles { get; }

        IReadOnlyList<string> PausedDeviceIdsFromMetering { get; }
    }

    public class AlertsManager : IAlertsManager
    {
        private readonly ISyncthingManager syncthingManager;
        private readonly IConflictFileWatcher conflictFileWatcher;
        private readonly IMeteredNetworkManager meteredNetworkManager;
        private readonly SynchronizedEventDispatcher eventDispatcher;

        public bool AnyWarnings => this.ConflictedFiles.Count > 0 || this.FoldersWithFailedTransferFiles.Count > 0;


        private IReadOnlyList<string> _conflictedFiles = EmptyReadOnlyList<string>.Instance;
        public IReadOnlyList<string> ConflictedFiles => this._enableConflictedFileAlerts ? this._conflictedFiles : EmptyReadOnlyList<string>.Instance;

        private IReadOnlyList<string> _foldersWithFailedTransferFiles = EmptyReadOnlyList<string>.Instance;
        public IReadOnlyList<string> FoldersWithFailedTransferFiles => this._enableFailedTransferAlerts ? this._foldersWithFailedTransferFiles : EmptyReadOnlyList<string>.Instance;

        public IReadOnlyList<string> PausedDeviceIdsFromMetering { get; private set; } = EmptyReadOnlyList<string>.Instance;

        public event EventHandler AlertsStateChanged;

        private bool _enableFailedTransferAlerts;
        public bool EnableFailedTransferAlerts
        {
            get => this._enableFailedTransferAlerts;
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
            get => this._enableConflictedFileAlerts;
            set
            {
                if (this._enableConflictedFileAlerts == value)
                    return;
                this._enableConflictedFileAlerts = value;
                this.OnAlertsStateChanged();
            }
        }

        public AlertsManager(ISyncthingManager syncthingManager, IConflictFileWatcher conflictFileWatcher, IMeteredNetworkManager meteredNetworkManager)
        {
            this.syncthingManager = syncthingManager;
            this.conflictFileWatcher = conflictFileWatcher;
            this.meteredNetworkManager = meteredNetworkManager;
            this.eventDispatcher = new SynchronizedEventDispatcher(this);

            this.syncthingManager.Folders.FolderErrorsChanged += this.FolderErrorsChanged;

            this.conflictFileWatcher.ConflictedFilesChanged += this.ConflictFilesChanged;

            this.meteredNetworkManager.PausedDevicesChanged += this.PausedDevicesChanged;
        }

        private void OnAlertsStateChanged()
        {
            this.eventDispatcher.Raise(this.AlertsStateChanged);
        }

        private void FolderErrorsChanged(object sender, FolderErrorsChangedEventArgs e)
        {
            var folders = this.syncthingManager.Folders.FetchAll();
            this._foldersWithFailedTransferFiles = folders.Where(x => x.FolderErrors.Any()).Select(x => x.Label).ToList().AsReadOnly();
 
            this.OnAlertsStateChanged();
        }

        private void ConflictFilesChanged(object sender, EventArgs e)
        {
            this._conflictedFiles = this.conflictFileWatcher.ConflictedFiles.ToList().AsReadOnly();

            this.OnAlertsStateChanged();
        }

        private void PausedDevicesChanged(object sender, EventArgs e)
        {
            this.PausedDeviceIdsFromMetering = this.meteredNetworkManager.PausedDevices.Select(x => x.DeviceId).ToList().AsReadOnly();

            this.OnAlertsStateChanged();
        }

        public void Dispose()
        {
            this.syncthingManager.Folders.FolderErrorsChanged -= this.FolderErrorsChanged;
            this.conflictFileWatcher.ConflictedFilesChanged -= this.ConflictFilesChanged;
            this.meteredNetworkManager.PausedDevicesChanged -= this.PausedDevicesChanged;
        }

        private static class EmptyReadOnlyList<T>
        {
            public static readonly IReadOnlyList<T> Instance = new List<T>().AsReadOnly();
        }
    }
}
