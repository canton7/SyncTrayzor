using System.Threading.Tasks;

namespace SyncTrayzor.Services.UpdateManagement
{
    public interface IUpdateVariantHandler
    {
        string VariantName { get; }
        bool CanAutoInstall { get; }
        bool RequiresUac { get; }

        Task<bool> TryHandleUpdateAvailableAsync(VersionCheckResults checkResult);
        void AutoInstall(string pathToRestartApplication);
    }
}
