using NLog;
using SyncTrayzor.Services.Config;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Services.UpdateManagement
{
    public interface IUpdateDownloader : IDisposable
    {
        Task<string> DownloadUpdateAsync(string url, Version version);
    }

    public class UpdateDownloader : IUpdateDownloader
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private const string downloadFileName = "SyncTrayzorUpdate-{0}.exe";

        private readonly string downloadsDir;
        private readonly IFilesystemProvider filesystemProvider;
        private readonly IInstallerCertificateVerifier installerVerifier;

        private FileStream downloadPathHandle;

        public UpdateDownloader(IApplicationPathsProvider pathsProvider, IFilesystemProvider filesystemProvider, IInstallerCertificateVerifier installerVerifier)
        {
            this.downloadsDir = pathsProvider.UpdatesDownloadPath;
            this.filesystemProvider = filesystemProvider;
            this.installerVerifier = installerVerifier;
        }

        public async Task<string> DownloadUpdateAsync(string url, Version version)
        {
            // The order is:
            // 1. Acquire lock
            // 2. Optionally, write
            // 3. Verify
            // 4. Run installer
            // 5. Release lock
            // This way we can be sure that no-one has the chance to change the file between us verifying it and us running the installer
            // If the already exists, but is corrupt, then we'll try again
            
            var downloadPath = Path.Combine(this.downloadsDir, String.Format(downloadFileName, version.ToString(3)));

            // Just in case...
            this.filesystemProvider.CreateDirectory(this.downloadsDir);

            this.downloadPathHandle = this.filesystemProvider.Open(downloadPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

            bool download = true;

            // Someone downloaded it already? Oh good. Let's see if it's corrupt or not...
            if (this.downloadPathHandle.Length > 0)
            {
                logger.Info("Skipping download as file {0} already exists", downloadPath);
                if (this.installerVerifier.Verify(downloadPath, this.downloadPathHandle))
                {
                    download = false;
                }
                else
                {
                    logger.Info("Actually, it's corrupt. Re-downloading");
                    this.downloadPathHandle.Position = 0;
                    this.downloadPathHandle.SetLength(0);
                }
            }
            
            if (download)
            {
                bool downloaded = await this.TryDownloadToFileAsync(this.downloadPathHandle, url);
                if (!downloaded)
                    return null;
            }

            logger.Info("Verifying...");

            if (!this.installerVerifier.Verify(downloadPath, this.downloadPathHandle))
            {
                logger.Warn("Download verification failed. Deleting {0}", downloadPath);

                this.downloadPathHandle.Close();
                this.downloadPathHandle = null;

                this.filesystemProvider.Delete(downloadPath);
                return null;
            }

            return downloadPath;
        }

        private async Task<bool> TryDownloadToFileAsync(FileStream downloadFileHandle, string url)
        {
            logger.Info("Downloading to {0}", downloadFileHandle.Name);

            // Temp file exists? Either a previous download was aborted, or there's another copy of us running somewhere
            // The difference depends on whether or not it's locked...
            try
            {
                var webClient = new WebClient();

                using (var downloadStream = await webClient.OpenReadTaskAsync(url))
                {
                    var responseLength = Int64.Parse(webClient.ResponseHeaders["Content-Length"]);
                    var previousDownloadProgressString = String.Empty;

                    var progress = new Progress<CopyToAsyncProgress>(p =>
                    {
                        var downloadProgressString = String.Format("Downloaded {0}/{1} ({2}%)",
                            FormatUtils.BytesToHuman(p.BytesRead), FormatUtils.BytesToHuman(responseLength), (p.BytesRead * 100) / responseLength);
                        if (downloadProgressString != previousDownloadProgressString)
                        {
                            logger.Info(downloadProgressString);
                            previousDownloadProgressString = downloadProgressString;
                        }
                    });

                    await downloadStream.CopyToAsync(downloadFileHandle, progress);
                }
            }
            catch (IOException e)
            {
                logger.Warn(String.Format("Failed to initiate download to temp file {0}", downloadFileHandle.Name), e);
                return false;
            }

            return true;
        }

        public void Dispose()
        {
            if (this.downloadPathHandle != null)
            {
                this.downloadPathHandle.Close();
                this.downloadPathHandle = null;
            }
        }
    }
}
