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
        public long BytesTransferred;
        public long TotalBytes { get; set; }

        public string FolderId { get; private set; }
        public string Path { get; private set; }

        public FileTransfer(string folderId, string path)
        {
            this.FolderId = folderId;
            this.Path = path;

            this.Status = FileTransferStatus.Started;
        }

        public void SetDownloadProgress(long bytesTransferred, long totalBytes)
        {
            this.BytesTransferred = bytesTransferred;
            this.TotalBytes = totalBytes;
            this.Status = FileTransferStatus.InProgress;
        }

        public void SetComplete()
        {
            this.Status = FileTransferStatus.Completed;
            this.BytesTransferred = this.TotalBytes;
        }
    }
}
