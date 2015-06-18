using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Services.UpdateManagement
{
    public class InstalledUpdateVariantHandler : IUpdateVariantHandler
    {
        private readonly IUpdateDownloader updateDownloader;
        private readonly IProcessStartProvider processStartProvider;
        private readonly IAssemblyProvider assemblyProvider;
        private readonly IApplicationState applicationState;

        private string installerPath;

        public string VariantName { get { return "installed"; } }
        public bool CanAutoInstall { get; private set; }

        public InstalledUpdateVariantHandler(
            IUpdateDownloader updateDownloader,
            IProcessStartProvider processStartProvider,
            IAssemblyProvider assemblyProvider,
            IApplicationState applicationState)
        {
            this.updateDownloader = updateDownloader;
            this.processStartProvider = processStartProvider;
            this.assemblyProvider = assemblyProvider;
            this.applicationState = applicationState;
        }

        public async Task<bool> TryHandleUpdateAvailableAsync(VersionCheckResults checkResult)
        {
            if (!String.IsNullOrWhiteSpace(checkResult.DownloadUrl) && !String.IsNullOrWhiteSpace(checkResult.Sha1sumDownloadUrl))
            {
                this.installerPath = await this.updateDownloader.DownloadUpdateAsync(checkResult.DownloadUrl, checkResult.Sha1sumDownloadUrl, checkResult.NewVersion);
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

        public void AutoInstall()
        {
            if (!this.CanAutoInstall)
                throw new InvalidOperationException("Auto-install not available");
            if (this.installerPath == null)
                throw new InvalidOperationException("TryHandleUpdateAvailableAsync returned false: cannot call AutoInstall");

            var path = String.Format("\"{0}\"", this.assemblyProvider.Location);
            if (!this.applicationState.HasMainWindow)
                path += " -minimized";

            this.processStartProvider.StartElevatedDetached(this.installerPath, "/SILENT", path);
        }
    }
}
