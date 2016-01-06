using Stylet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
