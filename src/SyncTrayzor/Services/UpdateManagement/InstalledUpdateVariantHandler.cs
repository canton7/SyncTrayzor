using System;
using System.Threading.Tasks;

namespace SyncTrayzor.Services.UpdateManagement
{
    public class InstalledUpdateVariantHandler : IUpdateVariantHandler
    {
        private const string updateDownloadFileName = "SyncTrayzorUpdate-{0}.exe";

        private readonly IUpdateDownloader updateDownloader;
        private readonly IProcessStartProvider processStartProvider;

        private string installerPath;

        public string VariantName => "installed";
        public bool RequiresUac => true;

        public bool CanAutoInstall { get; private set; }

        public InstalledUpdateVariantHandler(
            IUpdateDownloader updateDownloader,
            IProcessStartProvider processStartProvider)
        {
            this.updateDownloader = updateDownloader;
            this.processStartProvider = processStartProvider;
        }

        public async Task<bool> TryHandleUpdateAvailableAsync(VersionCheckResults checkResult)
        {
            if (!String.IsNullOrWhiteSpace(checkResult.DownloadUrl) && !String.IsNullOrWhiteSpace(checkResult.Sha512sumDownloadUrl))
            {
                this.installerPath = await this.updateDownloader.DownloadUpdateAsync(checkResult.DownloadUrl, checkResult.Sha512sumDownloadUrl, checkResult.NewVersion, updateDownloadFileName);
                this.CanAutoInstall = true;

                // If we return false, the upgrade will be aborted
                return this.installerPath != null;
            }
            else
            {
                // Can continue, but not auto-install
                this.CanAutoInstall = false;

                return true;
            }
        }

        public void AutoInstall(string pathToRestartApplication)
        {
            if (!this.CanAutoInstall)
                throw new InvalidOperationException("Auto-install not available");
            if (this.installerPath == null)
                throw new InvalidOperationException("TryHandleUpdateAvailableAsync returned false: cannot call AutoInstall");

            this.processStartProvider.StartElevatedDetached(this.installerPath, "/SILENT", pathToRestartApplication);
        }
    }
}
