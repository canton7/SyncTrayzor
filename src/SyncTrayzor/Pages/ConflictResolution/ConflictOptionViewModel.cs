using Stylet;
using SyncTrayzor.Services.Conflicts;
using SyncTrayzor.Utils;
using System;
using System.IO;

namespace SyncTrayzor.Pages.ConflictResolution
{
    public class ConflictOptionViewModel : PropertyChangedBase
    {
        public ConflictOption ConflictOption { get; }

        public string FileName => Path.GetFileName(this.ConflictOption.FilePath);

        public DateTime DateCreated => this.ConflictOption.Created;
        public DateTime LastModified => this.ConflictOption.LastModified;
        public string Size => FormatUtils.BytesToHuman(this.ConflictOption.SizeBytes, 1);
        public string ModifiedBy => this.ConflictOption.Device?.Name;

        public ConflictOptionViewModel(ConflictOption conflictOption)
        {
            this.ConflictOption = conflictOption;
        }
    }
}
