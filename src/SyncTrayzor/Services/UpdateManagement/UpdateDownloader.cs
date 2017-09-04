using NLog;
using SyncTrayzor.Services.Config;
using SyncTrayzor.Utils;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SyncTrayzor.Services.UpdateManagement
{
    public interface IUpdateDownloader
    {
        Task<string> DownloadUpdateAsync(string updateUrl, string sha512sumUrl, Version version, string downloadedFileNameTemplate);
    }

    public class UpdateDownloader : IUpdateDownloader
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static readonly TimeSpan fileMaxAge = TimeSpan.FromDays(3); // Arbitrary, but long
        private const string sha512sumDownloadFileName = "sha512sum-{0}.txt.asc";

        private readonly string downloadsDir;
        private readonly IFilesystemProvider filesystemProvider;
        private readonly IInstallerCertificateVerifier installerVerifier;

        public UpdateDownloader(IApplicationPathsProvider pathsProvider, IFilesystemProvider filesystemProvider, IInstallerCertificateVerifier installerVerifier)
        {
            this.downloadsDir = pathsProvider.UpdatesDownloadPath;
            this.filesystemProvider = filesystemProvider;
            this.installerVerifier = installerVerifier;
        }

        public async Task<string> DownloadUpdateAsync(string updateUrl, string sha512sumUrl, Version version, string downloadedFileNameTemplate)
        {
            var sha512sumDownloadPath = Path.Combine(this.downloadsDir, String.Format(sha512sumDownloadFileName, version.ToString(3)));
            var updateDownloadPath = Path.Combine(this.downloadsDir, String.Format(downloadedFileNameTemplate, version.ToString(3)));

            var sha512sumOutcome = await this.DownloadAndVerifyFileAsync<Stream>(sha512sumUrl, version, sha512sumDownloadPath, false, () =>
                {
                    var passed = this.installerVerifier.VerifySha512sum(sha512sumDownloadPath, out Stream sha512sumContents);
                    return (passed, sha512sumContents);
                });

            // Might be null, but if it's not make sure we dispose it (it's actually a MemoryStream, but let's be proper)
            bool updateSucceeded = false;
            using (var sha512sumContents = sha512sumOutcome.contents)
            {
                if (sha512sumOutcome.passed)
                {
                    updateSucceeded = (await this.DownloadAndVerifyFileAsync<object>(updateUrl, version, updateDownloadPath, false, () =>
                    {
                        var updateUri = new Uri(updateUrl);
                        // Make sure this is rewound - we might read from it multiple times
                        sha512sumOutcome.contents.Position = 0;
                        var updatePassed = this.installerVerifier.VerifyUpdate(updateDownloadPath, sha512sumOutcome.contents, updateUri.Segments.Last());
                        return (updatePassed, (object)null);
                    })).passed;
                }
            }

            this.CleanUpUnusedFiles();

            return updateSucceeded ? updateDownloadPath : null;
        }

        private async Task<(bool passed, T contents)> DownloadAndVerifyFileAsync<T>(string url, Version version, string downloadPath, bool deleteIfExists, Func<(bool passed, T contents)> verifier)
        {
            // This really needs refactoring to not be multiple-return...

            try
            {
                // Just in case...
                this.filesystemProvider.CreateDirectory(this.downloadsDir);

                // Someone downloaded it already? Oh good. Let's see if it's corrupt or not...
                if (this.filesystemProvider.FileExists(downloadPath) && !deleteIfExists)
                {
                    logger.Info("Skipping download as file {0} already exists", downloadPath);
                    var initialValidationResult = verifier();
                    if (initialValidationResult.passed)
                    {
                        // Touch the file, so we (or someone else!) doesn't delete when cleaning up
                        try
                        {
                            this.filesystemProvider.SetLastAccessTimeUtc(downloadPath, DateTime.UtcNow);
                        }
                        catch (Exception e)
                        {
                            logger.Warn(e, $"Failed to set last access time on {downloadPath}");
                        }

                        // EXIT POINT
                        return initialValidationResult;
                    }
                    else
                    {
                        if (!deleteIfExists)
                            logger.Info("Actually, it's corrupt. Re-downloading");
                        this.filesystemProvider.DeleteFile(downloadPath);
                    }
                }

                bool downloaded = await this.TryDownloadToFileAsync(downloadPath, url);
                if (!downloaded)
                {
                    logger.Warn("Problem downloading the file. Aborting");
                    // EXIT POINT
                    return (passed: false, contents: default(T));
                }

                logger.Info("Verifying...");

                var downloadedValidationResult = verifier();
                if (!downloadedValidationResult.passed)
                {
                    logger.Warn("Download verification failed. Deleting {0}", downloadPath);
                    this.filesystemProvider.DeleteFile(downloadPath);

                    // EXIT POINT
                    return (passed: false, contents: default(T));
                }

                // EXIT POINT
                logger.Info($"Downloaded validation result: {downloadedValidationResult}");
                return downloadedValidationResult;
            }
            catch (Exception e)
            {
                logger.Error(e, "Error in DownloadUpdateAsync");

                // EXIT POINT
                return (passed: false, contents: default(T));
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
                            logger.Debug(downloadProgressString);
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
            // TODO: Delete extracted portable dir?

            var threshold = DateTime.UtcNow - fileMaxAge;

            foreach (var file in this.filesystemProvider.GetFiles(this.downloadsDir))
            {
                if (this.filesystemProvider.GetLastAccessTimeUtc(Path.Combine(this.downloadsDir, file)) < threshold)
                {
                    try
                    {
                        this.filesystemProvider.DeleteFile(Path.Combine(this.downloadsDir, file));
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
