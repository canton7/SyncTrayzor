using Stylet;
using System.Collections.Generic;

namespace SyncTrayzor.Pages.BarAlerts
{
    public class FailedTransfersAlertViewModel : Screen, IBarAlert
    {
        public AlertSeverity Severity => AlertSeverity.Warning;

        public BindableCollection<string> FailingFolders { get; } = new BindableCollection<string>();

        public FailedTransfersAlertViewModel(IEnumerable<string> failingFolders)
        {
            this.FailingFolders.AddRange(failingFolders);
        }
    }
}
