using Stylet;
using SyncTrayzor.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.NotifyIcon
{
    public class UpgradeAvailableToastViewModel : Screen
    {
        public event EventHandler DownloadNowClicked;
        public event EventHandler RemindMeLaterClicked;
        public event EventHandler IgnoreClicked;

        public Version Version { get; set; }
        public string Changelog { get; set; }

        public UpgradeAvailableToastViewModel()
        {
        }

        public void DownloadNow()
        {
            var handler = this.DownloadNowClicked;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        public void RemindMeLater()
        {
            var handler = this.RemindMeLaterClicked;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        public void Ignore()
        {
            var handler = this.IgnoreClicked;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }
    }
}
