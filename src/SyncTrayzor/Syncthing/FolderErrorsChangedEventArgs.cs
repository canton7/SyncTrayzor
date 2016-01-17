using System;
using System.Collections.Generic;

namespace SyncTrayzor.Syncthing
{
    public class FolderErrorsChangedEventArgs : EventArgs
    {
        public string FolderId { get; }
        public IReadOnlyList<FolderError> Errors { get; }

        public FolderErrorsChangedEventArgs(string folderId, List<FolderError> folderErrors)
        {
            this.FolderId = folderId;
            this.Errors = folderErrors.AsReadOnly();
        }
    }
}
