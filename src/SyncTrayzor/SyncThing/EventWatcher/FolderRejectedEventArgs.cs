using System;

namespace SyncTrayzor.Syncthing.EventWatcher
{
    public class FolderRejectedEventArgs : EventArgs
    {
        public string DeviceId { get; }
        public string FolderId { get; }

        public FolderRejectedEventArgs(string deviceId, string folderId)
        {
            this.DeviceId = deviceId;
            this.FolderId = folderId;
        }
    }
}
