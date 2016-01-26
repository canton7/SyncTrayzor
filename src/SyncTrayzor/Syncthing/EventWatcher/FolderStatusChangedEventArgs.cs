using SyncTrayzor.Syncthing.ApiClient;
using System;

namespace SyncTrayzor.Syncthing.EventWatcher
{
    public class FolderStatusChangedEventArgs : EventArgs
    {
        public string FolderId { get; }

        public FolderStatus FolderStatus { get; }

        public FolderStatusChangedEventArgs(string folderId, FolderStatus folderStatus)
        {
            this.FolderId = folderId;
            this.FolderStatus = folderStatus;
        }
    }
}
