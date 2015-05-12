using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.TransferHistory
{
    public class FileTransferChangedEventArgs : EventArgs
    {
        public FileTransfer FileTransfer { get; private set; }

        public FileTransferChangedEventArgs(FileTransfer fileTransfer)
        {
            this.FileTransfer = fileTransfer;
        }
    }
}
