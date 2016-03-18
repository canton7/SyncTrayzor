using NLog;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace SyncTrayzor.Services.UpdateManagement
{
    public class VersionCheckResults
    {
        public Version NewVersion { get; }
        public string DownloadUrl { get; }
        public string Sha512sumDownloadUrl { get; }
        public string ReleaseNotes { get; }
        public string ReleasePageUrl { get; }

        public VersionCheckResults(
            Version newVersion,
            string downloadUrl,
            string sha512sumDownloadUrl,
            string releaseNotes,
            string releasePageUrl)
        {
            this.NewVersion = newVersion;
            this.DownloadUrl = downloadUrl;
            this.Sha512sumDownloadUrl = sha512sumDownloadUrl;
            this.ReleaseNotes = releaseNotes;
            this.ReleasePageUrl = releasePageUrl;
        }

        public override string ToString()
        {
            return $"<VersionCheckResults NewVersion={this.NewVersion} DownloadUrl={this.DownloadUrl} Sha512sumDownloadUrl={this.Sha512sumDownloadUrl} " +
                $"ReleaseNotes={this.ReleaseNotes} ReleasePageUrl={this.ReleasePageUrl}>";
        }
    }

    public interface IUpdateChecker
    {
        Task<VersionCheckResults> FetchUpdateAsync();
        Task<VersionCheckResults> CheckForAcceptableUpdateAsync(Version latestIgnoredVersion = null);
    }

    public class UpdateChecker : IUpdateChecker
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly Dictionary<ProcessorArchitecture, string> processorArchitectureToStringMap = new Dictionary<ProcessorArchitecture, string>()
        {
            { ProcessorArchitecture.Amd64, "x64" },
            { ProcessorArchitecture.Arm, "arm" },
            { ProcessorArchitecture.IA64, "x64" },
            { ProcessorArchitecture.MSIL, "msil" },
            { ProcessorArchitecture.None, "none" },
            { ProcessorArchitecture.X86, "x86" }
        };

        private readonly Version applicationVersion;
        private readonly ProcessorArchitecture processorArchitecture;
        private readonly string variant;
        private readonly IUpdateNotificationClient updateNotificationClient;

        public UpdateChecker(
            Version applicationVersion,
            ProcessorArchitecture processorArchitecture,
            string variant,
            IUpdateNotificationClient updateNotificationClient)
        {
            this.applicationVersion = applicationVersion;
            this.processorArchitecture = processorArchitecture;
            this.variant = variant;
            this.updateNotificationClient = updateNotificationClient;
        }

        public async Task<VersionCheckResults> FetchUpdateAsync()
        {
            // We don't care if we fail
            try
            {
                var update = await this.updateNotificationClient.FetchUpdateAsync(
                    this.applicationVersion.ToString(3),
                    processorArchitectureToStringMap[this.processorArchitecture],
                    this.variant);

                if (update == null)
                {
                    logger.Info("No updates found");
                    return null;
                }

                if (update.Error != null)
                {
                    logger.Warn("Update API returned an error. Code: {0} Message: {1}", update.Error.Code, update.Error.Message);
                    return null;
                }

                var updateData = update.Data;
                if (updateData == null)
                {
                    logger.Info("No updates available");
                    return null;
                }

                var results = new VersionCheckResults(updateData.Version, updateData.DirectDownloadUrl, update.Data.Sha512sumDownloadUrl, updateData.ReleaseNotes, updateData.ReleasePageUrl);
                logger.Info("Found new version: {0}", results);
                return results;
            }
            catch (Exception e)
            {
                logger.Warn(e, "Fetching updates failed with an error");
                return null;
            }
        }
        
        public async Task<VersionCheckResults> CheckForAcceptableUpdateAsync(Version latestIgnoredVersion)
        {
            var results = await this.FetchUpdateAsync();

            if (results == null)
                return null;

            if (results.NewVersion <= this.applicationVersion)
                return null;

            if (latestIgnoredVersion != null && results.NewVersion <= latestIgnoredVersion)
                return null;

            return results;
        }
    }
}
