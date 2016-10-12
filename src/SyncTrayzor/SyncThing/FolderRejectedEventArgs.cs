using SyncTrayzor.Syncthing.Devices;
using SyncTrayzor.Syncthing.Folders;
using System;

namespace SyncTrayzor.Syncthing
{
    public class FolderRejectedEventArgs : EventArgs
    {
        public Device Device { get; }
        public Folder Folder { get; }

        public FolderRejectedEventArgs(Device device, Folder folder)
        {
            this.Device = device;
            this.Folder = folder;
        }
    }
}
