using Stylet;
using SyncTrayzor.Localization;
using SyncTrayzor.Properties;
using SyncTrayzor.Syncthing;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;

namespace SyncTrayzor.Utils
{
    public static class SafeSyncthingExtensions
    {
        public static async Task StartWithErrorDialogAsync(this ISyncthingManager syncthingManager, IWindowManager windowManager)
        {
            try
            {
                await syncthingManager.StartAsync();
            }
            catch (Win32Exception e)
            {
                if (e.ErrorCode != -2147467259)
                    throw;

                // Possibly "This program is blocked by group policy. For more information, contact your system administrator" caused
                // by e.g. CryptoLocker?
                windowManager.ShowMessageBox(
                    Localizer.F(Resources.Dialog_SyncthingBlockedByGroupPolicy_Message, e.Message, syncthingManager.ExecutablePath),
                    Resources.Dialog_SyncthingBlockedByGroupPolicy_Title,
                    MessageBoxButton.OK, icon: MessageBoxImage.Error);
            }
            catch (SyncthingDidNotStartCorrectlyException e)
            {
                windowManager.ShowMessageBox(
                    Localizer.F(Resources.Dialog_SyncthingDidNotStart_Message, e.Message),
                    Resources.Dialog_SyncthingDidNotStart_Title,
                    MessageBoxButton.OK, icon: MessageBoxImage.Error);
            }
        }
    }
}
