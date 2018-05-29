using SyncTrayzor.Syncthing.ApiClient;
using SyncTrayzor.Syncthing.Folders;
using System;

namespace SyncTrayzor.Syncthing.TransferHistory
{
    public class FileTransfer
    {
        public FileTransferStatus Status { get; set; }

        public long BytesTransferred { get; private set; }
        public long TotalBytes { get; private set; }
        public double? DownloadBytesPerSecond { get; private set; }

        public Folder Folder { get; }
        public string Path { get; }
        public ItemChangedItemType ItemType { get; }
        public ItemChangedActionType ActionType { get; }

        public DateTime StartedUtc { get; private set; }
        public DateTime? FinishedUtc { get; private set; }

        public string Error { get; private set; }
        public bool IsNewError { get; private set; }

        private DateTime? lastProgressUpdateUtc;

        public FileTransfer(Folder folder, string path, ItemChangedItemType itemType, ItemChangedActionType actionType)
        {
            this.Folder = folder;
            this.Path = path;

            this.Status = FileTransferStatus.Started;
            this.StartedUtc = DateTime.UtcNow;
            this.ItemType = itemType;
            this.ActionType = actionType;
        }

        public void SetDownloadProgress(long bytesTransferred, long totalBytes)
        {
            var now = DateTime.UtcNow;
            if (this.lastProgressUpdateUtc.HasValue)
            {
                var deltaBytesTransferred = bytesTransferred - this.BytesTransferred;
                this.DownloadBytesPerSecond = deltaBytesTransferred / (now - this.lastProgressUpdateUtc.Value).TotalSeconds;
            }

            this.BytesTransferred = bytesTransferred;
            this.TotalBytes = totalBytes;
            this.Status = FileTransferStatus.InProgress;
            this.lastProgressUpdateUtc = now;
        }

        public void SetComplete(string error, bool isNewError)
        {
            this.Status = FileTransferStatus.Completed;
            this.BytesTransferred = this.TotalBytes;
            this.FinishedUtc = DateTime.UtcNow;
            this.Error = error;
            this.IsNewError = isNewError;
        }

        public override string ToString()
        {
            return $"<FileTransfer Folder={this.Folder.Label} Path={this.Path} Status={this.Status} ItemType={this.ItemType} ActionType={this.ActionType} Started={this.StartedUtc} Finished={this.FinishedUtc}>";
        }
    }
}
