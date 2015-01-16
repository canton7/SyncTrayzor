using Stylet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.NotifyIcon
{
    public class NotifyIconViewModel : PropertyChangedBase
    {
        public bool Visible { get; set; }

        public event EventHandler WindowOpenRequested;
        public event EventHandler ExitRequested;

        public void DoubleClick()
        {
            this.OnWindowOpenRequested();
        }

        public void Exit()
        {
            this.OnExitRequested();
        }

        private void OnWindowOpenRequested()
        {
            var handler = this.WindowOpenRequested;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        private void OnExitRequested()
        {
            var handler = this.ExitRequested;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }
    }
}
