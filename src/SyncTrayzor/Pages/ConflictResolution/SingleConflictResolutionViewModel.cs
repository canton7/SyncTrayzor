using Stylet;

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
