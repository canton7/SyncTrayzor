using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stylet;
using SyncTrayzor.Services;
using SyncTrayzor.Services.Conflicts;
using System.IO;
using SyncTrayzor.Localization;
using SyncTrayzor.Properties;
using System.Windows;

namespace SyncTrayzor.Pages.ConflictResolution
{
    public class SingleConflictResolutionViewModel : Screen
    {
        public ConflictViewModel Conflict { get; set; }

        public ConflictResolutionViewModel Delegate { get; set; }

        public void ShowFileInFolder()
        {
            this.Delegate.ShowFileInFolder(this.Conflict);
        }

        public void ChooseOriginal()
        {
            this.Delegate.ChooseOriginal(this.Conflict);
        }

        public void ChooseConflictFile(ConflictOptionViewModel conflictOption)
        {
            this.Delegate.ChooseConflictFile(this.Conflict, conflictOption);
        }
    }
}
