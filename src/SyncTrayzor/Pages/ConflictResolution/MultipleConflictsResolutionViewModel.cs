using Stylet;
using SyncTrayzor.Utils;
using System.Collections.Generic;

namespace SyncTrayzor.Pages.ConflictResolution
{
    public class MultipleConflictsResolutionViewModel : Screen
    {
        public List<ConflictViewModel> Conflicts { get; set; }

        public ConflictResolutionViewModel Delegate { get; set; }

        public void ChooseOriginal()
        {
            foreach (var conflict in this.Conflicts)
            {
                this.Delegate.ChooseOriginal(conflict);
            }
        }

        public void ChooseNewest()
        {
            foreach(var conflict in this.Conflicts)
            {
                var newestOption = conflict.ConflictOptions.MaxBy(x => x.DateCreated);
                if (newestOption.DateCreated > conflict.LastModified)
                    this.Delegate.ChooseConflictFile(conflict, newestOption);
                else
                    this.Delegate.ChooseOriginal(conflict);
            }
        }

        public void ChooseNewestConflict()
        {
            foreach (var conflict in this.Conflicts)
            {
                var newestOption = conflict.ConflictOptions.MaxBy(x => x.DateCreated);
                this.Delegate.ChooseConflictFile(conflict, newestOption);
            }
        }
    }
}
