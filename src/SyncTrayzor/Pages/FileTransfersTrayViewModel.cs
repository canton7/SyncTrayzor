using Stylet;
using SyncTrayzor.Properties.Strings;
using SyncTrayzor.SyncThing;
using SyncTrayzor.SyncThing.TransferHistory;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SyncTrayzor.Pages
{
    public class FileTransferViewModel : PropertyChangedBase
    {
        public readonly FileTransfer FileTransfer;
        private readonly DispatcherTimer completedTimeAgoUpdateTimer;

        public string FolderId { get; private set; }
        public string Path { get; private set; }
        public Icon Icon { get; private set; }
        public string Error { get; private set; }
        public bool WasDeleted { get; private set; }
        
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
        public bool IsStarting { get; private set; }
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
            this.FolderId = this.FileTransfer.FolderId;
            this.Path = this.FileTransfer.Path;
            this.Icon = ShellTools.GetIcon(this.FileTransfer.Path, this.FileTransfer.ItemType == SyncThing.EventWatcher.ItemChangedItemType.File);
            this.WasDeleted = this.FileTransfer.ActionType == SyncThing.EventWatcher.ItemChangedActionType.Delete;

            this.UpdateState();
        }

        public void UpdateState()
        {
            switch (this.FileTransfer.Status)
            {
                case FileTransferStatus.Started:
                    this.ProgressString = Resources.FileTransfersTrayView_Starting;
                    this.IsStarting = true;
                    this.ProgressPercent = 0;
                    break;

                case FileTransferStatus.InProgress:
                    this.ProgressString = String.Format(Resources.FileTransfersTrayView_Downloading,
                        FormatUtils.BytesToHuman(this.FileTransfer.BytesTransferred),
                        FormatUtils.BytesToHuman(this.FileTransfer.TotalBytes));
                    this.IsStarting = false;
                    this.ProgressPercent = ((float)this.FileTransfer.BytesTransferred / (float)this.FileTransfer.TotalBytes) * 100;
                    break;
            }

            this.Error = this.FileTransfer.Error;
        }
    }

    public class FileTransfersTrayViewModel : Screen
    {
        private readonly ISyncThingManager syncThingManager;

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

        public FileTransfersTrayViewModel(ISyncThingManager syncThingManager)
        {
            this.syncThingManager = syncThingManager;

            this.CompletedTransfers = new BindableCollection<FileTransferViewModel>();
            this.InProgressTransfers = new BindableCollection<FileTransferViewModel>();

            this.CompletedTransfers.CollectionChanged += (o, e) => { this.NotifyOfPropertyChange(() => this.HasCompletedTransfers); this.NotifyOfPropertyChange(() => this.AnyTransfers); };
            this.InProgressTransfers.CollectionChanged += (o, e) => { this.NotifyOfPropertyChange(() => this.HasInProgressTransfers); this.NotifyOfPropertyChange(() => this.AnyTransfers); };
        }

        protected override void OnActivate()
        {
            foreach (var completedTransfer in this.syncThingManager.TransferHistory.CompletedTransfers.Take(10).Reverse())
            {
                this.CompletedTransfers.Add(new FileTransferViewModel(completedTransfer));
            }

            foreach (var inProgressTranser in this.syncThingManager.TransferHistory.InProgressTransfers.Reverse())
            {
                this.InProgressTransfers.Add(new FileTransferViewModel(inProgressTranser));
            }

            this.syncThingManager.TransferHistory.TransferStarted += this.TransferStarted;
            this.syncThingManager.TransferHistory.TransferCompleted += this.TransferCompleted;
            this.syncThingManager.TransferHistory.TransferStateChanged += this.TransferStateChanged;

            this.UpdateConnectionStats(this.syncThingManager.TotalConnectionStats);

            this.syncThingManager.TotalConnectionStatsChanged += this.TotalConnectionStatsChanged;
        }

        protected override void OnDeactivate()
        {
            this.syncThingManager.TransferHistory.TransferStarted -= this.TransferStarted;
            this.syncThingManager.TransferHistory.TransferCompleted -= this.TransferCompleted;
            this.syncThingManager.TransferHistory.TransferStateChanged -= this.TransferStateChanged;

            this.syncThingManager.TotalConnectionStatsChanged -= this.TotalConnectionStatsChanged;

            this.CompletedTransfers.Clear();
            this.InProgressTransfers.Clear();
        }

        private void TransferStarted(object sender, FileTransferChangedEventArgs e)
        {
            this.InProgressTransfers.Insert(0, new FileTransferViewModel(e.FileTransfer));
        }

        private void TransferCompleted(object sender, FileTransferChangedEventArgs e)
        {
            var transferVm = this.InProgressTransfers.First(x => x.FileTransfer == e.FileTransfer);
            this.InProgressTransfers.Remove(transferVm);
            this.CompletedTransfers.Insert(0, transferVm);
            transferVm.UpdateState();
        }

        private void TransferStateChanged(object sender, FileTransferChangedEventArgs e)
        {
            var transferVm = this.InProgressTransfers.FirstOrDefault(x => x.FileTransfer == e.FileTransfer);
            if (transferVm != null)
                transferVm.UpdateState();
        }

        private void TotalConnectionStatsChanged(object sender, ConnectionStatsChangedEventArgs e)
        {
            this.UpdateConnectionStats(e.TotalConnectionStats);
        }

        private void UpdateConnectionStats(SyncThingConnectionStats connectionStats)
        {
            if (connectionStats == null)
            {
                this.InConnectionRate = "0.0KB";
                this.OutConnectionRate = "0.0KB";
            }
            else
            {
                this.InConnectionRate = FormatUtils.BytesToHuman(connectionStats.InBytesPerSecond, 1);
                this.OutConnectionRate = FormatUtils.BytesToHuman(connectionStats.OutBytesPerSecond, 1);
            }
        }
    }
}
