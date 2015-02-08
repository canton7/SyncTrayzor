using Stylet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.NotifyIcon
{
    public interface INotifyIconDelegate : IScreen
    {
        void CloseToTray();
        void RestoreFromTray();
        void Shutdown();
    }
}
