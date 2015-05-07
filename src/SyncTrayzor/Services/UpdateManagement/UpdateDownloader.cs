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
    public interface IUpdateDownloader
    {
        Task<string> DownloadUpdateAsync(string url, Version version);
    }

    public class UpdateDownloader : IUpdateDownloader
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static readonly TimeSpan fileMaxAge = TimeSpan.FromDays(3); // Arbitrary, but long
        private const string downloadFileName = "SyncTrayzorUpdate-{0}.exe";

        private readonly string downloadsDir;
        private readonly IFilesystemProvider filesystemProvider;
        private readonly IInstallerCertificateVerifier installerVerifier;

        public UpdateDownloader(IApplicationPathsProvider pathsProvider, IFilesystemProvider filesystemProvider, IInstallerCertificateVerifier installerVerifier)
        {
            this.downloadsDir = pathsProvider.UpdatesDownloadPath;
            this.filesystemProvider = filesystemProvider;
            this.installerVerifier = installerVerifier;
        }

        public async Task<string> DownloadUpdateAsync(string url, Version version)
        {
            try
            {
                var downloadPath = Path.Combine(this.downloadsDir, String.Format(downloadFileName, version.ToString(3)));

                // Just in case...
                this.filesystemProvider.CreateDirectory(this.downloadsDir);

                bool download = true;

                // Someone downloaded it already? Oh good. Let's see if it's corrupt or not...
                if (this.filesystemProvider.Exists(downloadPath))
                {
                    logger.Info("Skipping download as file {0} already exists", downloadPath);
                    if (this.installerVerifier.Verify(downloadPath))
                    {
                        download = false;
                        // Touch the file, so we (or someone else!) doesn't delete when cleaning up
                        this.filesystemProvider.SetLastAccessTimeUtc(downloadPath, DateTime.UtcNow);
                    }
                    else
                    {
                        logger.Info("Actually, it's corrupt. Re-downloading");
                        this.filesystemProvider.Delete(downloadPath);
                    }
                }

                // House-keeping. Do this now, after SetLastAccessTimeUTc has been called, but before we start hitting the early-exits
                this.CleanUpUnusedFiles();

                if (download)
                {
                    bool downloaded = await this.TryDownloadToFileAsync(downloadPath, url);
                    if (!downloaded)
                        return null;

                    logger.Info("Verifying...");

                    if (!this.installerVerifier.Verify(downloadPath))
                    {
                        logger.Warn("Download verification failed. Deleting {0}", downloadPath);
                        this.filesystemProvider.Delete(downloadPath);
                        return null;
                    }
                }

                return downloadPath;
            }
            catch (Exception e)
            {
                logger.Error("Error in DownloadUpdateAsync", e);
                return null;
            }
        }

        private async Task<bool> TryDownloadToFileAsync(string downloadPath, string url)
        {
            logger.Info("Downloading to {0}", downloadPath);

            // Temp file exists? Either a previous download was aborted, or there's another copy of us running somewhere
            // The difference depends on whether or not it's locked...
            try
            {
                var webClient = new WebClient();

                using (var downloadFileHandle = this.filesystemProvider.Open(downloadPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
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
                logger.Warn(String.Format("Failed to initiate download to temp file {0}", downloadPath), e);
                return false;
            }

            return true;
        }

        private void CleanUpUnusedFiles()
        {
            var threshold = DateTime.UtcNow - fileMaxAge;

            foreach (var file in this.filesystemProvider.GetFiles(this.downloadsDir))
            {
                if (this.filesystemProvider.GetLastAccessTimeUtc(Path.Combine(this.downloadsDir, file)) < threshold)
                {
                    try
                    {
                        this.filesystemProvider.Delete(Path.Combine(this.downloadsDir, file));
                        logger.Info("Deleted old file {0}", file);
                    }
                    catch (IOException e)
                    {
                        logger.Warn(String.Format("Failed to delete old file {0}", file), e);
                    }
                }
            }
        }
    }
}
