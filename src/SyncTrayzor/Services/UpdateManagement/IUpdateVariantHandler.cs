using System.Threading.Tasks;

namespace SyncTrayzor.Services.UpdateManagement
{
    public interface IUpdateVariantHandler
    {
        string VariantName { get; }
        bool CanAutoInstall { get; }

        Task<bool> TryHandleUpdateAvailableAsync(VersionCheckResults checkResult);
        void AutoInstall();
    }
}
