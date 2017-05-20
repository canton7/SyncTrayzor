using NLog;
using System;
using Pri.LongPath;

namespace SyncTrayzor.Services.Config
{
    public interface IApplicationPathsProvider
    {
        string LogFilePath { get; }
        string SyncthingBackupPath { get; }
        string ConfigurationFilePath { get; }
        string ConfigurationFileBackupPath { get; }
        string UpdatesDownloadPath { get; }
        string InstallCountFilePath { get; }
        string CefCachePath { get; }
        string DefaultSyncthingPath { get; }
        string DefaultSyncthingHomePath { get; }

        string UnexpandedDefaultSyncthingPath { get; }

        void Initialize(PathConfiguration pathConfiguration);
    }

    public class ApplicationPathsProvider : IApplicationPathsProvider
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IPathTransformer pathTransformer;

        public string LogFilePath { get; private set; }
        public string SyncthingBackupPath { get; private set; }
        public string ConfigurationFilePath { get; private set; }
        public string ConfigurationFileBackupPath { get; private set; }
        public string CefCachePath { get; private set; }
        public string UpdatesDownloadPath { get; private set; }
        public string InstallCountFilePath { get; private set; }
        public string DefaultSyncthingPath { get; private set; }
        public string DefaultSyncthingHomePath { get; private set; }

        // Needed by migrations in the ConfigurationProvider
        public string UnexpandedDefaultSyncthingPath { get; private set; }

        public ApplicationPathsProvider(IPathTransformer pathTransformer)
        {
            this.pathTransformer = pathTransformer;
        }

        public void Initialize(PathConfiguration pathConfiguration)
        {
            if (pathConfiguration == null)
                throw new ArgumentNullException(nameof(pathConfiguration));

            this.LogFilePath = this.pathTransformer.MakeAbsolute(pathConfiguration.LogFilePath);
            this.SyncthingBackupPath = this.pathTransformer.MakeAbsolute("syncthing.exe");
            this.ConfigurationFilePath = this.pathTransformer.MakeAbsolute(pathConfiguration.ConfigurationFilePath);
            this.ConfigurationFileBackupPath = this.pathTransformer.MakeAbsolute(pathConfiguration.ConfigurationFileBackupPath);
            this.CefCachePath = this.pathTransformer.MakeAbsolute(pathConfiguration.CefCachePath);
            this.UpdatesDownloadPath = Path.Combine(Path.GetTempPath(), "SyncTrayzor");
            this.InstallCountFilePath = this.pathTransformer.MakeAbsolute("InstallCount.txt");
            this.DefaultSyncthingPath = String.IsNullOrWhiteSpace(pathConfiguration.SyncthingPath) ?
                null :
                this.pathTransformer.MakeAbsolute(pathConfiguration.SyncthingPath);
            this.DefaultSyncthingHomePath = String.IsNullOrWhiteSpace(pathConfiguration.SyncthingHomePath) ?
                null :
                this.pathTransformer.MakeAbsolute(pathConfiguration.SyncthingHomePath);
            this.UnexpandedDefaultSyncthingPath = pathConfiguration.SyncthingPath;

            logger.Debug("LogFilePath: {0}", this.LogFilePath);
            logger.Debug("SyncthingBackupPath: {0}", this.SyncthingBackupPath);
            logger.Debug("ConfigurationFilePath: {0}", this.ConfigurationFilePath);
            logger.Debug("ConfigurationFileBackupPath: {0}", this.ConfigurationFileBackupPath);
            logger.Debug("CefCachePath: {0}", this.CefCachePath);
            logger.Debug("DefaultSyncthingPath: {0}", this.DefaultSyncthingPath);
            logger.Debug("DefaultSyncthingHomePath: {0}", this.DefaultSyncthingHomePath);
        }
    }
}
