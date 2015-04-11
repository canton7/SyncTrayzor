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

        private string installerPath;

        public string VariantName { get { return "installed"; } }
        public bool CanAutoInstall { get { return true; } }

        public InstalledUpdateVariantHandler(IUpdateDownloader updateDownloader, IProcessStartProvider processStartProvider)
        {
            this.updateDownloader = updateDownloader;
            this.processStartProvider = processStartProvider;
        }

        public async Task<bool> TryHandleUpdateAvailableAsync(VersionCheckResults checkResult)
        {
            this.installerPath = await this.updateDownloader.DownloadUpdateAsync(checkResult.DownloadUrl, checkResult.NewVersion);
            return this.installerPath != null;
        }

        public void AutoInstall()
        {
            this.processStartProvider.StartDetached(this.installerPath);
        }
    }
}
