using Pri.LongPath;
using Stylet;
using SyncTrayzor.Services;
using SyncTrayzor.Syncthing;
using SyncTrayzor.Syncthing.ApiClient;
using SyncTrayzor.Syncthing.TransferHistory;
using SyncTrayzor.Utils;
using System;
using System.Linq;

namespace SyncTrayzor.Pages.Tray
{
    public class FileTransfersTrayViewModel : Screen, IDisposable
    {
        private const int initialCompletedTransfersToDisplay = 100;

        private readonly ISyncthingManager syncthingManager;
        private readonly IProcessStartProvider processStartProvider;

        public NetworkGraphViewModel NetworkGraph { get; }

        public BindableCollection<FileTransferViewModel> CompletedTransfers { get; private set; }
        public BindableCollection<FileTransferViewModel> InProgressTransfers { get; private set; }

        public bool HasCompletedTransfers => this.CompletedTransfers.Count > 0;
        public bool HasInProgressTransfers => this.InProgressTransfers.Count > 0;

        public string InConnectionRate { get; private set; }
        public string OutConnectionRate { get; private set; }

        public bool AnyTransfers => this.HasCompletedTransfers || this.HasInProgressTransfers;

        public FileTransfersTrayViewModel(ISyncthingManager syncthingManager, IProcessStartProvider processStartProvider, NetworkGraphViewModel networkGraph)
        {
            this.syncthingManager = syncthingManager;
            this.processStartProvider = processStartProvider;

            this.syncthingManager.StateChanged += this.SyncthingStateChanged;

            this.NetworkGraph = networkGraph;
            this.NetworkGraph.ConductWith(this);

            this.CompletedTransfers = new BindableCollection<FileTransferViewModel>();
            this.InProgressTransfers = new BindableCollection<FileTransferViewModel>();

            this.CompletedTransfers.CollectionChanged += (o, e) => { this.NotifyOfPropertyChange(() => this.HasCompletedTransfers); this.NotifyOfPropertyChange(() => this.AnyTransfers); };
            this.InProgressTransfers.CollectionChanged += (o, e) => { this.NotifyOfPropertyChange(() => this.HasInProgressTransfers); this.NotifyOfPropertyChange(() => this.AnyTransfers); };
        }

        protected override void OnActivate()
        {
            foreach (var completedTransfer in this.syncthingManager.TransferHistory.CompletedTransfers.Take(initialCompletedTransfersToDisplay).Reverse())
            {
                this.CompletedTransfers.Add(new FileTransferViewModel(completedTransfer));
            }

            foreach (var inProgressTranser in this.syncthingManager.TransferHistory.InProgressTransfers.Where(x => x.Status == FileTransferStatus.InProgress).Reverse())
            {
                this.InProgressTransfers.Add(new FileTransferViewModel(inProgressTranser));
            }

            // We start caring about samples when they're either finished, or have a progress update
            this.syncthingManager.TransferHistory.TransferStateChanged += this.TransferStateChanged;

            this.UpdateConnectionStats(this.syncthingManager.TotalConnectionStats);

            this.syncthingManager.TotalConnectionStatsChanged += this.TotalConnectionStatsChanged;
        }

        protected override void OnDeactivate()
        {
            this.syncthingManager.TransferHistory.TransferStateChanged -= this.TransferStateChanged;

            this.syncthingManager.TotalConnectionStatsChanged -= this.TotalConnectionStatsChanged;

            this.CompletedTransfers.Clear();
            this.InProgressTransfers.Clear();
        }

        private void TransferStateChanged(object sender, FileTransferChangedEventArgs e)
        {
            var transferVm = this.InProgressTransfers.FirstOrDefault(x => x.FileTransfer == e.FileTransfer);
            if (transferVm == null)
            {
                if (e.FileTransfer.Status == FileTransferStatus.Completed)
                    this.CompletedTransfers.Insert(0, new FileTransferViewModel(e.FileTransfer));
                else if (e.FileTransfer.Status == FileTransferStatus.InProgress)
                    this.InProgressTransfers.Insert(0, new FileTransferViewModel(e.FileTransfer));
                // We don't care about 'starting' transfers
            }
            else
            {
                transferVm.UpdateState();

                if (e.FileTransfer.Status == FileTransferStatus.Completed)
                {
                    this.InProgressTransfers.Remove(transferVm);
                    this.CompletedTransfers.Insert(0, transferVm);
                }
            }
        }

        private void TotalConnectionStatsChanged(object sender, ConnectionStatsChangedEventArgs e)
        {
            this.UpdateConnectionStats(e.TotalConnectionStats);
        }

        private void SyncthingStateChanged(object sender, SyncthingStateChangedEventArgs e)
        {
            if (this.syncthingManager.State == SyncthingState.Running)
                this.UpdateConnectionStats(0, 0);
            else
                this.UpdateConnectionStats(null, null);
        }

        private void UpdateConnectionStats(SyncthingConnectionStats connectionStats)
        {
            if (this.syncthingManager.State == SyncthingState.Running)
                this.UpdateConnectionStats(connectionStats.InBytesPerSecond, connectionStats.OutBytesPerSecond);
            else
                this.UpdateConnectionStats(null, null);
        }

        private void UpdateConnectionStats(double? inBytesPerSecond, double? outBytesPerSecond)
        {
            if (inBytesPerSecond == null)
                this.InConnectionRate = null;
            else
                this.InConnectionRate = FormatUtils.BytesToHuman(inBytesPerSecond.Value, 1);

            if (outBytesPerSecond == null)
                this.OutConnectionRate = null;
            else
                this.OutConnectionRate = FormatUtils.BytesToHuman(outBytesPerSecond.Value, 1);
        }

        public void ItemClicked(FileTransferViewModel fileTransferVm)
        {
            var fileTransfer = fileTransferVm.FileTransfer;
            if (!this.syncthingManager.Folders.TryFetchById(fileTransfer.FolderId, out var folder))
                return; // Huh? Nothing we can do about it...

            // Not sure of the best way to deal with deletions yet...
            if (fileTransfer.ActionType == ItemChangedActionType.Update)
            {
                if (fileTransfer.ItemType == ItemChangedItemType.File)
                    this.processStartProvider.ShowFileInExplorer(Path.Combine(folder.Path, fileTransfer.Path));
                else if (fileTransfer.ItemType == ItemChangedItemType.Dir)
                    this.processStartProvider.ShowFolderInExplorer(Path.Combine(folder.Path, fileTransfer.Path));
            }
        }

        public void Dispose()
        {
            this.syncthingManager.StateChanged -= this.SyncthingStateChanged;

            this.NetworkGraph.Dispose();
        }
    }
}
