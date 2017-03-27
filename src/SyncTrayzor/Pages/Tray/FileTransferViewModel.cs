using Stylet;
using SyncTrayzor.Properties;
using SyncTrayzor.Syncthing.ApiClient;
using SyncTrayzor.Syncthing.TransferHistory;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SyncTrayzor.Pages.Tray
{
    public class FileTransferViewModel : PropertyChangedBase
    {
        public readonly FileTransfer FileTransfer;
        private readonly DispatcherTimer completedTimeAgoUpdateTimer;

        public string Path { get; }
        public string FolderId { get; }
        public string FullPath { get; }
        public ImageSource Icon { get; }
        public string Error { get; private set; }
        public bool WasDeleted { get; }

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
            using (var icon = ShellTools.GetIcon(this.FileTransfer.Path, this.FileTransfer.ItemType != ItemChangedItemType.Dir))
            {
                var bs = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                bs.Freeze();
                this.Icon = bs;
            }
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
}
