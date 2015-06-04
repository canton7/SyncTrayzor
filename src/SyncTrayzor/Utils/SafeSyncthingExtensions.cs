using Stylet;
using SyncTrayzor.Localization;
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
        public static async Task StartWithErrorDialogAsync(this ISyncThingManager syncThingManager, IWindowManager windowManager)
        {
            try
            {
                await syncThingManager.StartAsync();
            }
            catch (Win32Exception e)
            {
                if (e.ErrorCode != -2147467259)
                    throw;

                // Possibly "This program is blocked by group policy. For more information, contact your system administrator" caused
                // by e.g. CryptoLocker?
                windowManager.ShowMessageBox(
                    Localizer.T("Dialog_SyncthingBlockedByGroupPolicy_Message", e.Message, syncThingManager.ExecutablePath),
                    Localizer.T("Dialog_SyncthingBlockedByGroupPolicy_Title"),
                    MessageBoxButton.OK, icon: MessageBoxImage.Error);
            }
            catch (SyncThingDidNotStartCorrectlyException e)
            {
                windowManager.ShowMessageBox(
                    Localizer.T("Dialog_SyncthingDidNotStart_Message", e.Message),
                    Localizer.T("Dialog_SyncthingDidNotStart_Title"),
                    MessageBoxButton.OK, icon: MessageBoxImage.Error);
            }
        }
    }
}
