using SyncTrayzor.Syncthing.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Syncthing
{
    public class FolderRejectedEventArgs : EventArgs
    {
        public Device Device { get; }
        public string FolderId { get; }

        public FolderRejectedEventArgs(Device device, string folderId)
        {
            this.Device = device;
            this.FolderId = folderId;
        }
    }
}
