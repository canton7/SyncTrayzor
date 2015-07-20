using NLog;
using SyncTrayzor.Services.Config;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Services.UpdateManagement
{
    public interface IUpdateDownloader
    {
        Task<string> DownloadUpdateAsync(string updateUrl, string sha1sumUrl, Version version);
    }

    public class UpdateDownloader : IUpdateDownloader
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static readonly TimeSpan fileMaxAge = TimeSpan.FromDays(3); // Arbitrary, but long
        private const string updateDownloadFileName = "SyncTrayzorUpdate-{0}.exe";
        private const string sham1sumDownloadFileName = "sha1sum-{0}.txt.asc";

        private readonly string downloadsDir;
        private readonly IFilesystemProvider filesystemProvider;
        private readonly IInstallerCertificateVerifier installerVerifier;

        public UpdateDownloader(IApplicationPathsProvider pathsProvider, IFilesystemProvider filesystemProvider, IInstallerCertificateVerifier installerVerifier)
        {
            this.downloadsDir = pathsProvider.UpdatesDownloadPath;
            this.filesystemProvider = filesystemProvider;
            this.installerVerifier = installerVerifier;
        }

        public async Task<string> DownloadUpdateAsync(string updateUrl, string sha1sumUrl, Version version)
        {
            var sha1sumDownloadPath = Path.Combine(this.downloadsDir, String.Format(sham1sumDownloadFileName, version.ToString(3)));
            var updateDownloadPath = Path.Combine(this.downloadsDir, String.Format(updateDownloadFileName, version.ToString(3)));

            var sha1sumOutcome = await this.DownloadAndVerifyFileAsync<Stream>(sha1sumUrl, version, sha1sumDownloadPath, () =>
                {
                    Stream sha1sumContents;
                    var passed = this.installerVerifier.VerifySha1sum(sha1sumDownloadPath, out sha1sumContents);
                    return Tuple.Create(passed, sha1sumContents);
                });

            // Might be null, but if it's not make sure we dispose it (it's actually a MemoryStream, but let's be proper)
            bool updateSucceeded = false;
            using (var sha1sumContents = sha1sumOutcome.Item2)
            {
                if (sha1sumOutcome.Item1)
                {
                    updateSucceeded = (await this.DownloadAndVerifyFileAsync<object>(updateUrl, version, updateDownloadPath, () =>
                    {
                        var updateUri = new Uri(updateUrl);
                        // Make sure this is rewound - we might read from it multiple times
                        sha1sumOutcome.Item2.Position = 0;
                        var updatePassed = this.installerVerifier.VerifyUpdate(updateDownloadPath, sha1sumOutcome.Item2, updateUri.Segments.Last());
                        return Tuple.Create(updatePassed, (object)null);
                    })).Item1;
                }
            }

            this.CleanUpUnusedFiles();

            return updateSucceeded ? updateDownloadPath : null;
        }

        private async Task<Tuple<bool, T>> DownloadAndVerifyFileAsync<T>(string url, Version version, string downloadPath, Func<Tuple<bool, T>> verifier)
        {
            // This really needs refactoring to not be multiple-return...

            try
            {
                // Just in case...
                this.filesystemProvider.CreateDirectory(this.downloadsDir);

                // Someone downloaded it already? Oh good. Let's see if it's corrupt or not...
                if (this.filesystemProvider.Exists(downloadPath))
                {
                    logger.Info("Skipping download as file {0} already exists", downloadPath);
                    var initialValidationResult = verifier();
                    if (initialValidationResult.Item1)
                    {
                        // Touch the file, so we (or someone else!) doesn't delete when cleaning up
                        this.filesystemProvider.SetLastAccessTimeUtc(downloadPath, DateTime.UtcNow);

                        // EXIT POINT
                        return initialValidationResult;
                    }
                    else
                    {
                        logger.Info("Actually, it's corrupt. Re-downloading");
                        this.filesystemProvider.Delete(downloadPath);
                    }
                }

                bool downloaded = await this.TryDownloadToFileAsync(downloadPath, url);
                if (!downloaded)
                {
                    // EXIT POINT
                    return Tuple.Create(false, default(T));
                }

                logger.Info("Verifying...");

                var downloadedValidationResult = verifier();
                if (!downloadedValidationResult.Item1)
                {
                    logger.Warn("Download verification failed. Deleting {0}", downloadPath);
                    this.filesystemProvider.Delete(downloadPath);

                    // EXIT POINT
                    return Tuple.Create(false, default(T));
                }

                // EXIT POINT
                return downloadedValidationResult;
            }
            catch (Exception e)
            {
                logger.Error("Error in DownloadUpdateAsync", e);

                // EXIT POINT
                return Tuple.Create(false, default(T));
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
                logger.Warn($"Failed to initiate download to temp file {downloadPath}", e);
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
                        logger.Warn($"Failed to delete old file {file}", e);
                    }
                }
            }
        }
    }
}
