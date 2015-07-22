using System;
using System.Collections.Generic;

namespace SyncTrayzor.SyncThing.TransferHistory
{
    public class FolderSynchronizationFinishedEventArgs : EventArgs
    {
        public string FolderId { get; }
        public IReadOnlyList<FileTransfer> FileTransfers { get; }

        public FolderSynchronizationFinishedEventArgs(string folderId, List<FileTransfer> fileTransfers)
        {
            this.FolderId = folderId;
            this.FileTransfers = fileTransfers.AsReadOnly();
        }
    }
}
