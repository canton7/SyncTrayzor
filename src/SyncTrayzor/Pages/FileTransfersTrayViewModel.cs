using Pri.LongPath;
using Stylet;
using SyncTrayzor.Properties.Strings;
using SyncTrayzor.Services;
using SyncTrayzor.SyncThing;
using SyncTrayzor.SyncThing.ApiClient;
using SyncTrayzor.SyncThing.TransferHistory;
using SyncTrayzor.Utils;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Threading;

namespace SyncTrayzor.Pages
{
    public class FileTransferViewModel : PropertyChangedBase
    {
        public readonly FileTransfer FileTransfer;
        private readonly DispatcherTimer completedTimeAgoUpdateTimer;

        public string Path { get; }
        public string FolderId { get; }
        public string FullPath { get; }
        public Icon Icon { get; }
        public string Error { get; private set; }
        public bool WasDeleted { get;  }

        public DateTime Completed => this.FileTransfer.FinishedUtc.GetValueOrDefault().ToLocalTime();

        public string CompletedTimeAgo
        {
            get
            {
                if (this.FileTransfer.FinishedUtc.HasValue)
                    return FormatUtils.TimeSpanToTimeAgo(DateTime.UtcNow - this.FileTransfer.FinishedUtc.Value);
                else
                    return null;
            }
        }

        public string ProgressString { get; private set; }
        public float ProgressPercent { get; private set; }

        public FileTransferViewModel(FileTransfer fileTransfer)
        {
            this.completedTimeAgoUpdateTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMinutes(1),
            };
            this.completedTimeAgoUpdateTimer.Tick += (o, e) => this.NotifyOfPropertyChange(() => this.CompletedTimeAgo);
            this.completedTimeAgoUpdateTimer.Start();

            this.FileTransfer = fileTransfer;
            this.Path = Pri.LongPath.Path.GetFileName(this.FileTransfer.Path);
            this.FullPath = this.FileTransfer.Path;
            this.FolderId = this.FileTransfer.FolderId;
            this.Icon = ShellTools.GetIcon(this.FileTransfer.Path, this.FileTransfer.ItemType != ItemChangedItemType.Folder);
            this.WasDeleted = this.FileTransfer.ActionType == ItemChangedActionType.Delete;

            this.UpdateState();
        }

        public void UpdateState()
        {
            switch (this.FileTransfer.Status)
            {
                case FileTransferStatus.InProgress:
                    if (this.FileTransfer.DownloadBytesPerSecond.HasValue)
                    {
                        this.ProgressString = String.Format(Resources.FileTransfersTrayView_Downloading_RateKnown,
                            FormatUtils.BytesToHuman(this.FileTransfer.BytesTransferred),
                            FormatUtils.BytesToHuman(this.FileTransfer.TotalBytes),
                            FormatUtils.BytesToHuman(this.FileTransfer.DownloadBytesPerSecond.Value, 1));
                    }
                    else
                    {
                        this.ProgressString = String.Format(Resources.FileTransfersTrayView_Downloading_RateUnknown,
                            FormatUtils.BytesToHuman(this.FileTransfer.BytesTransferred),
                            FormatUtils.BytesToHuman(this.FileTransfer.TotalBytes));
                    }
                    
                    this.ProgressPercent = ((float)this.FileTransfer.BytesTransferred / (float)this.FileTransfer.TotalBytes) * 100;
                    break;

                case FileTransferStatus.Completed:
                    this.ProgressPercent = 100;
                    this.ProgressString = null;
                    break;
            }

            this.Error = this.FileTransfer.Error;
        }
    }

    public class FileTransfersTrayViewModel : Screen
    {
        private const int initialCompletedTransfersToDisplay = 100;

        private readonly ISyncThingManager syncThingManager;
        private readonly IProcessStartProvider processStartProvider;

        public BindableCollection<FileTransferViewModel> CompletedTransfers { get; private set; }
        public BindableCollection<FileTransferViewModel> InProgressTransfers { get; private set; }

        public bool HasCompletedTransfers
        {
            get { return this.CompletedTransfers.Count > 0; }
        }
        public bool HasInProgressTransfers
        {
            get { return this.InProgressTransfers.Count > 0; }
        }

        public string InConnectionRate { get; private set; }
        public string OutConnectionRate { get; private set; }

        public bool AnyTransfers
        {
            get { return this.HasCompletedTransfers || this.HasInProgressTransfers; }
        }

        public FileTransfersTrayViewModel(ISyncThingManager syncThingManager, IProcessStartProvider processStartProvider)
        {
            this.syncThingManager = syncThingManager;
            this.processStartProvider = processStartProvider;

            this.CompletedTransfers = new BindableCollection<FileTransferViewModel>();
            this.InProgressTransfers = new BindableCollection<FileTransferViewModel>();

            this.CompletedTransfers.CollectionChanged += (o, e) => { this.NotifyOfPropertyChange(() => this.HasCompletedTransfers); this.NotifyOfPropertyChange(() => this.AnyTransfers); };
            this.InProgressTransfers.CollectionChanged += (o, e) => { this.NotifyOfPropertyChange(() => this.HasInProgressTransfers); this.NotifyOfPropertyChange(() => this.AnyTransfers); };
        }

        protected override void OnActivate()
        {
            foreach (var completedTransfer in this.syncThingManager.TransferHistory.CompletedTransfers.Take(initialCompletedTransfersToDisplay).Reverse())
            {
                this.CompletedTransfers.Add(new FileTransferViewModel(completedTransfer));
            }

            foreach (var inProgressTranser in this.syncThingManager.TransferHistory.InProgressTransfers.Where(x => x.Status == FileTransferStatus.InProgress).Reverse())
            {
                this.InProgressTransfers.Add(new FileTransferViewModel(inProgressTranser));
            }

            // We start caring about samples when they're either finished, or have a progress update
            this.syncThingManager.TransferHistory.TransferStateChanged += this.TransferStateChanged;

            this.UpdateConnectionStats(this.syncThingManager.TotalConnectionStats);

            this.syncThingManager.TotalConnectionStatsChanged += this.TotalConnectionStatsChanged;
        }

        protected override void OnDeactivate()
        {
            this.syncThingManager.TransferHistory.TransferStateChanged -= this.TransferStateChanged;

            this.syncThingManager.TotalConnectionStatsChanged -= this.TotalConnectionStatsChanged;

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

        private void UpdateConnectionStats(SyncThingConnectionStats connectionStats)
        {
            if (connectionStats == null)
            {
                this.InConnectionRate = "0.0B";
                this.OutConnectionRate = "0.0B";
            }
            else
            {
                this.InConnectionRate = FormatUtils.BytesToHuman(connectionStats.InBytesPerSecond, 1);
                this.OutConnectionRate = FormatUtils.BytesToHuman(connectionStats.OutBytesPerSecond, 1);
            }
        }

        public void ItemClicked(FileTransferViewModel fileTransferVm)
        {
            var fileTransfer = fileTransferVm.FileTransfer;
            Folder folder;
            if (!this.syncThingManager.Folders.TryFetchById(fileTransfer.FolderId, out folder))
                return; // Huh? Nothing we can do about it...

            // Not sure of the best way to deal with deletions yet...
            if (fileTransfer.ActionType == ItemChangedActionType.Update)
            {
                if (fileTransfer.ItemType == ItemChangedItemType.File)
                    this.processStartProvider.StartDetached("explorer.exe", $"/select, \"{ Path.Combine(folder.Path, fileTransfer.Path)}\"");
                else if (fileTransfer.ItemType == ItemChangedItemType.Folder)
                    this.processStartProvider.StartDetached("explorer.exe", Path.Combine(folder.Path, fileTransfer.Path));
            }
        }
    }
}
