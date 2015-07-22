using System;

namespace SyncTrayzor.SyncThing.TransferHistory
{
    public class FileTransferChangedEventArgs : EventArgs
    {
        public FileTransfer FileTransfer { get; }

        public FileTransferChangedEventArgs(FileTransfer fileTransfer)
        {
            this.FileTransfer = fileTransfer;
        }
    }
}
