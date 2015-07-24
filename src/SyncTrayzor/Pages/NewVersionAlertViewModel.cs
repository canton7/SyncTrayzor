using Stylet;
using System;

namespace SyncTrayzor.Pages
{
    public class NewVersionAlertViewModel : Screen
    {
        public bool CanInstall { get; set; }
        public Version Version { get; set; }
        public string Changelog { get; set; }

        public bool DontRemindMe { get; private set; }

        public void Download()
        {
            this.RequestClose(true);
        }

        public void Install()
        {
            this.RequestClose(true);
        }

        public void RemindLater()
        {
            this.RequestClose(false);
        }

        public void DontRemind()
        {
            this.DontRemindMe = true;
            this.RequestClose(false);
        }
    }
}
