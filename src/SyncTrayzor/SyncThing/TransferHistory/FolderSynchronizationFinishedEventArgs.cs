using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.TransferHistory
{
    public class FolderSynchronizationFinishedEventArgs : EventArgs
    {
        public string FolderId { get; private set; }
        public IReadOnlyList<FileTransfer> FileTransfers { get; private set; }

        public FolderSynchronizationFinishedEventArgs(string folderId, List<FileTransfer> fileTransfers)
        {
            this.FolderId = folderId;
            this.FileTransfers = fileTransfers.AsReadOnly();
        }
    }
}
