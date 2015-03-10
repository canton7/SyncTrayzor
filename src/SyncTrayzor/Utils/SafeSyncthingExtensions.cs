using Stylet;
using SyncTrayzor.SyncThing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SyncTrayzor.Utils
{
    public static class SafeSyncthingExtensions
    {
        public static void StartWithErrorDialog(this ISyncThingManager syncThingManager, IWindowManager windowManager)
        {
            try
            {
                syncThingManager.Start();
            }
            catch (Win32Exception e)
            {
                if (e.ErrorCode != -2147467259)
                    throw;

                // Possibly "This program is blocked by group policy. For more information, contact your system administrator" caused
                // by e.g. CryptoLocker?
                var msg = String.Format("Unable to start Syncthing: {0}\n\nThis could be because Windows if set up to forbid executing files " +
                    "in AppData, or because you have anti-malware installed (e.g. CryptoPrevent ) which prevents executing files in AppData.\n\n" +
                    "Please adjust your settings / whitelists to allow '{1}' to execute", e.Message, syncThingManager.ExecutablePath);
                windowManager.ShowMessageBox(msg, "Error starting Syncthing", MessageBoxButton.OK, icon: MessageBoxImage.Error);
            }
        }
    }
}
