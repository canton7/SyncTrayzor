using NLog;
using Stylet;
using SyncTrayzor.Pages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SyncTrayzor.Services.UpdateManagement
{
    public class VersionCheckResults
    {
        public Version NewVersion { get; private set; }
        public string DownloadUrl { get; private set; }
        public string Sha1sumDownloadUrl { get; private set; }
        public string ReleaseNotes { get; private set; }
        public string ReleasePageUrl { get; private set; }

        public VersionCheckResults(
            Version newVersion,
            string downloadUrl,
            string sha1sumDownloadUrl,
            string releaseNotes,
            string releasePageUrl)
        {
            this.NewVersion = newVersion;
            this.DownloadUrl = downloadUrl;
            this.Sha1sumDownloadUrl = sha1sumDownloadUrl;
            this.ReleaseNotes = releaseNotes;
            this.ReleasePageUrl = releasePageUrl;
        }

        public override string ToString()
        {
            return String.Format("<VersionCheckResults NewVersion={0} DownloadUrl={1} Sha1sumDownloadUrl={2} ReleaseNotes={3} ReleasePageUrl={4}>",
                this.NewVersion, this.DownloadUrl, this.Sha1sumDownloadUrl, this.ReleaseNotes, this.ReleasePageUrl);
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

                var results = new VersionCheckResults(updateData.Version, updateData.DirectDownloadUrl, update.Data.Sha1sumDownloadUrl, updateData.ReleaseNotes, updateData.ReleasePageUrl);
                logger.Info("Found new version: {0}", results);
                return results;
            }
            catch (Exception e)
            {
                logger.Warn("Fetching updates failed with an error", e);
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
