using Stylet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Pages.BarAlerts
{
    public class ConflictsAlertViewModel : PropertyChangedBase, IBarAlert
    {
        public AlertSeverity Severity => AlertSeverity.Warning;

        public int NumConflicts { get; }

        public ConflictsAlertViewModel(int numConflicts)
        {
            this.NumConflicts = numConflicts;
        }
    }
}
