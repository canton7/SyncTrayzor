using SyncTrayzor.Syncthing.Folders;
using System;
using System.Collections.Generic;

namespace SyncTrayzor.Syncthing.TransferHistory
{
    public class FolderSynchronizationFinishedEventArgs : EventArgs
    {
        public Folder Folder { get; }
        public IReadOnlyList<FileTransfer> FileTransfers { get; }

        public FolderSynchronizationFinishedEventArgs(Folder folder, List<FileTransfer> fileTransfers)
        {
            this.Folder = folder;
            this.FileTransfers = fileTransfers.AsReadOnly();
        }
    }
}
