using SyncTrayzor.SyncThing.ApiClient;
using System;

namespace SyncTrayzor.SyncThing
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
