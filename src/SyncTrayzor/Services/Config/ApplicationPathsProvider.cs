using NLog;
using SyncTrayzor.Utils;
using System;
using System.IO;

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

        void Initialize(PathConfiguration pathConfiguration);
    }

    public class ApplicationPathsProvider : IApplicationPathsProvider
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private PathConfiguration pathConfiguration;

        public ApplicationPathsProvider(IAssemblyProvider assemblyProvider)
        {
            this.ExePath = Path.GetDirectoryName(assemblyProvider.Location);
        }

        public void Initialize(PathConfiguration pathConfiguration)
        {
            if (pathConfiguration == null)
                throw new ArgumentNullException("pathConfiguration");

            this.pathConfiguration = pathConfiguration;

            logger.Debug("ExePath: {0}", this.ExePath);
            logger.Debug("LogFilePath: {0}", this.LogFilePath);
            logger.Debug("SyncthingBackupPath: {0}", this.SyncthingBackupPath);
            logger.Debug("ConfigurationFilePath: {0}", this.ConfigurationFilePath);
            logger.Debug("ConfigurationFileBackupPath: {0}", this.ConfigurationFileBackupPath);
            logger.Debug("CefCachePath: {0}", this.CefCachePath);
        }

        public string ExePath { get; set; }

        public string LogFilePath => EnvVarTransformer.Transform(this.pathConfiguration.LogFilePath);

        public string SyncthingBackupPath => Path.Combine(this.ExePath, "syncthing.exe");

        public string ConfigurationFilePath =>  EnvVarTransformer.Transform(this.pathConfiguration.ConfigurationFilePath);

        public string ConfigurationFileBackupPath => EnvVarTransformer.Transform(this.pathConfiguration.ConfigurationFileBackupPath);

        public string CefCachePath => EnvVarTransformer.Transform(this.pathConfiguration.CefCachePath);

        public string UpdatesDownloadPath =>  Path.Combine(Path.GetTempPath(), "SyncTrayzor");

        public string InstallCountFilePath =>  Path.Combine(this.ExePath, "InstallCount.txt");
    }
}
