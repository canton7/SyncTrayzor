using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace SyncTrayzor.Services.Config
{
    public class ConfigFolderCleaner
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IApplicationPathsProvider applicationPathsProvider;
        private readonly IFilesystemProvider filesystemProvider;

        public ConfigFolderCleaner(IApplicationPathsProvider applicationPathsProvider, IFilesystemProvider filesystemProvider)
        {
            this.applicationPathsProvider = applicationPathsProvider;
            this.filesystemProvider = filesystemProvider;
        }

        public void Clean()
        {
            // We used to have a 'logs archive' folder in the root - that's no longer used, in favour of 'logs/logs archive'
            var oldLogArchivesPath = Path.Combine(Path.GetDirectoryName(this.applicationPathsProvider.LogFilePath), "logs archive");
            if (this.filesystemProvider.DirectoryExists(oldLogArchivesPath))
            {
                logger.Info("Deleting old logs archive path: {0}", oldLogArchivesPath);
                this.filesystemProvider.DeleteDirectory(oldLogArchivesPath, true);
            }

            // Delete 'SyncTrayzor.log' and 'syncthing.log' in the root
            var oldSyncTrayzorRootLogPath = Path.Combine(Path.GetDirectoryName(this.applicationPathsProvider.ConfigurationFilePath), "SyncTrayzor.log");
            if (this.filesystemProvider.FileExists(oldSyncTrayzorRootLogPath))
            {
                logger.Info("Deleting old SyncTrayzor log file: {0}", oldSyncTrayzorRootLogPath);
                this.filesystemProvider.DeleteFile(oldSyncTrayzorRootLogPath);
            }

            var oldSyncthingRootLogPath = Path.Combine(Path.GetDirectoryName(this.applicationPathsProvider.ConfigurationFilePath), "syncthing.log");
            if (this.filesystemProvider.FileExists(oldSyncthingRootLogPath))
            {
                logger.Info("Deleting old Syncthing log file: {0}", oldSyncthingRootLogPath);
                this.filesystemProvider.DeleteFile(oldSyncthingRootLogPath);
            }

            // Delete 'syncthing.log' files from within the log files path
            var syncthingLogPath = Path.Combine(this.applicationPathsProvider.LogFilePath, "syncthing.log");
            if (this.filesystemProvider.FileExists(syncthingLogPath))
            {
                logger.Info("Deleting old Syncthing log path: {0}", syncthingLogPath);
                this.filesystemProvider.DeleteFile(syncthingLogPath);
            }

            // Delete all 'syncthing.x.log' files from within the log file archive path
            foreach (var file in this.filesystemProvider.GetFiles(Path.Combine(this.applicationPathsProvider.LogFilePath, "logs archive"), "syncthing.*.log", SearchOption.TopDirectoryOnly))
            {
                logger.Info("Deleting old Syncthing log archive: {0}", file);
                this.filesystemProvider.DeleteFile(file);
            }
        }
    }
}
