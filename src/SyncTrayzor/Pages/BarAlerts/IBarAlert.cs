using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Pages.BarAlerts
{
    public interface IBarAlert
    {
        AlertSeverity Severity { get; }
    }
}
