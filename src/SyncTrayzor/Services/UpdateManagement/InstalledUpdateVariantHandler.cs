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

        private string installerPath;

        public InstalledUpdateVariantHandler(IUpdateDownloader updateDownloader)
        {
            this.updateDownloader = updateDownloader;
        }

        public async Task<bool> TryHandleUpdateAvailableAsync(VersionCheckResults checkResult)
        {
            this.installerPath = await this.updateDownloader.DownloadUpdateAsync(checkResult.DownloadUrl, checkResult.NewVersion);
            return this.installerPath != null;
        }
    }
}
