using Stylet;
using SyncTrayzor.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Pages.ConflictResolution
{
    public class ConflictOptionViewModel : PropertyChangedBase
    {
        public ConflictOption ConflictOption { get; }

        public string FileName => Path.GetFileName(this.ConflictOption.FilePath);

        public DateTime DateCreated => this.ConflictOption.Created;
        public DateTime LastModified => this.ConflictOption.LastModified;

        public ConflictOptionViewModel(ConflictOption conflictOption)
        {
            this.ConflictOption = conflictOption;
        }
    }
}
