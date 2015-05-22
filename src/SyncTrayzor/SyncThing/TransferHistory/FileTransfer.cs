using SyncTrayzor.SyncThing.EventWatcher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.TransferHistory
{
    public class FileTransfer
    {
        public FileTransferStatus Status { get; set; }

        public long BytesTransferred { get; private set; }
        public long TotalBytes { get; private set; }

        public string FolderId { get; private set; }
        public string Path { get; private set; }
        public ItemChangedItemType ItemType { get; private set; }
        public ItemChangedActionType ActionType { get; private set; }

        public DateTime StartedUtc { get; private set; }
        public DateTime? FinishedUtc { get; private set; }

        public string Error { get; private set; }

        public FileTransfer(string folderId, string path, ItemChangedItemType itemType, ItemChangedActionType actionType)
        {
            this.FolderId = folderId;
            this.Path = path;

            this.Status = FileTransferStatus.Started;
            this.StartedUtc = DateTime.UtcNow;
            this.ItemType = itemType;
            this.ActionType = actionType;
        }

        public void SetDownloadProgress(long bytesTransferred, long totalBytes)
        {
            this.BytesTransferred = bytesTransferred;
            this.TotalBytes = totalBytes;
            this.Status = FileTransferStatus.InProgress;
        }

        public void SetComplete(string error)
        {
            this.Status = FileTransferStatus.Completed;
            this.BytesTransferred = this.TotalBytes;
            this.FinishedUtc = DateTime.UtcNow;
            this.Error = error;
        }
    }
}
