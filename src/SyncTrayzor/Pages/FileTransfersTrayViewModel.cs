using Stylet;
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
            this.Icon = ShellTools.GetIcon(this.FileTransfer.Path);

            this.UpdateState();
        }

        public void UpdateState()
        {
            switch (this.FileTransfer.Status)
            {
                case FileTransferStatus.Started:
                    this.ProgressString = "Starting...";
                    this.IsStarting = true;
                    this.ProgressPercent = 0;
                    break;

                case FileTransferStatus.InProgress:
                    this.ProgressString = String.Format("Downloading {0}/{1}",
                        FormatUtils.BytesToHuman(this.FileTransfer.BytesTransferred),
                        FormatUtils.BytesToHuman(this.FileTransfer.TotalBytes));
                    this.IsStarting = false;
                    this.ProgressPercent = ((float)this.FileTransfer.BytesTransferred / (float)this.FileTransfer.TotalBytes) * 100;
                    break;
            }
        }
    }

    public class FileTransfersTrayViewModel : Screen
    {
        private readonly ISyncThingTransferHistory transferHistory;

        public BindableCollection<FileTransferViewModel> CompletedTransfers { get; private set; }
        public BindableCollection<FileTransferViewModel> InProgressTransfers { get; private set; }

        public FileTransfersTrayViewModel(ISyncThingManager syncThingManager)
        {
            this.transferHistory = syncThingManager.TransferHistory;

            this.CompletedTransfers = new BindableCollection<FileTransferViewModel>();
            this.InProgressTransfers = new BindableCollection<FileTransferViewModel>();
        }

        protected override void OnActivate()
        {
            foreach (var completedTransfer in this.transferHistory.CompletedTransfers.Take(10))
            {
                this.CompletedTransfers.Add(new FileTransferViewModel(completedTransfer));
            }

            foreach (var inProgressTranser in this.transferHistory.InProgressTransfers)
            {
                this.InProgressTransfers.Add(new FileTransferViewModel(inProgressTranser));
            }

            this.transferHistory.TransferStarted += this.TransferStarted;
            this.transferHistory.TransferCompleted += this.TransferCompleted;
            this.transferHistory.TransferStateChanged += this.TransferStateChanged;
        }

        protected override void OnDeactivate()
        {
            this.transferHistory.TransferStarted -= this.TransferStarted;
            this.transferHistory.TransferCompleted -= this.TransferCompleted;
            this.transferHistory.TransferStateChanged -= this.TransferStateChanged;

            this.CompletedTransfers.Clear();
            this.InProgressTransfers.Clear();
        }

        private void TransferStarted(object sender, FileTransferChangedEventArgs e)
        {
            this.InProgressTransfers.Add(new FileTransferViewModel(e.FileTransfer));
        }

        private void TransferCompleted(object sender, FileTransferChangedEventArgs e)
        {
            var transferVm = this.InProgressTransfers.First(x => x.FileTransfer == e.FileTransfer);
            this.InProgressTransfers.Remove(transferVm);
            this.CompletedTransfers.Add(transferVm);
            transferVm.UpdateState();
        }

        private void TransferStateChanged(object sender, FileTransferChangedEventArgs e)
        {
            var transferVm = this.InProgressTransfers.FirstOrDefault(x => x.FileTransfer == e.FileTransfer);
            if (transferVm != null)
                transferVm.UpdateState();
        }
    }
}
