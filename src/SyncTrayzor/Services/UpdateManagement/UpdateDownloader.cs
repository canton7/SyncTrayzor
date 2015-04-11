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

        private const string downloadFileTempName = "SyncTrayzorUpdate-{0}.exe.temp";
        private const string downloadFileName = "SyncTrayzorUpdate-{0}.exe";

        private readonly string downloadsDir;
        private readonly IFilesystemProvider filesystemProvider;

        public UpdateDownloader(IApplicationPathsProvider pathsProvider, IFilesystemProvider filesystemProvider)
        {
            this.downloadsDir = pathsProvider.UpdatesDownloadPath;
            this.filesystemProvider = filesystemProvider;
        }

        public async Task<string> DownloadUpdateAsync(string url, Version version)
        {
            var tempPath = Path.Combine(this.downloadsDir, String.Format(downloadFileTempName, version.ToString(3)));
            var finalPath = Path.Combine(this.downloadsDir, String.Format(downloadFileName, version.ToString(3)));

            // Someone downloaded it already? Oh good.
            if (this.filesystemProvider.Exists(finalPath))
            {
                logger.Info("Skipping download as final file {0} already exists", finalPath);
                return finalPath;
            }

            // Just in case...
            this.filesystemProvider.CreateDirectory(this.downloadsDir);

            // Temp file exists? Either a previous download was aborted, or there's another copy of us running somewhere
            // The difference depends on whether or not it's locked...
            try
            {
                var webClient = new WebClient();

                logger.Info("Downloading to {0}", tempPath);
                using (var fileStream = this.filesystemProvider.Open(tempPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
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

                    await downloadStream.CopyToAsync(fileStream, progress);
                }
            }
            catch (IOException e)
            {
                logger.Warn(String.Format("Failed to initiate download to temp file {0}", tempPath), e);
                return null;
            }

            // Possible, I guess, that the finalPath now exists. If it does, that's fine
            try
            {
                logger.Info("Copying temp file {0} to {1}", tempPath, finalPath);
                this.filesystemProvider.Move(tempPath, finalPath);
            }
            catch (IOException e)
            {
                logger.Warn(String.Format("Failed to move temp file {0} to final file {1}", tempPath, finalPath), e);
                return null;
            }

            this.filesystemProvider.Delete(tempPath);

            logger.Info("Done");

            return finalPath;
        }
    }
}
