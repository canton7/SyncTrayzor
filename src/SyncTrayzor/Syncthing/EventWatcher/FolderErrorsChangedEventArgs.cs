using SyncTrayzor.Syncthing.ApiClient;
using System;
using System.Collections.Generic;

namespace SyncTrayzor.Syncthing.EventWatcher
{
    public class FolderErrorsChangedEventArgs : EventArgs
    {
        public string FolderId { get; }

        public List<FolderErrorData> Errors { get; }

        public FolderErrorsChangedEventArgs(string folderId, List<FolderErrorData> errors)
        {
            this.FolderId = folderId;
            this.Errors = errors;
        }
    }
}
