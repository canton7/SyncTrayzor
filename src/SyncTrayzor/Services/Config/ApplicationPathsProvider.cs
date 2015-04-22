using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Services.Config
{
    public interface IApplicationPathsProvider
    {
        string LogFilePath { get; }
        string SyncthingPath { get; }
        string SyncthingCustomHomePath { get; }
        string SyncthingBackupPath { get; }
        string ConfigurationFilePath { get; }
        string ConfigurationFileBackupPath { get; }
        string UpdatesDownloadPath { get; }
        string InstallCountFilePath { get; }

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
            logger.Debug("SyncthingCustomHomePath: {0}", this.SyncthingCustomHomePath);
            logger.Debug("SyncThingPath: {0}", this.SyncthingPath);
            logger.Debug("SyncThingBackupPath: {0}", this.SyncthingBackupPath);
            logger.Debug("ConfigurationFilePath: {0}", this.ConfigurationFilePath);
            logger.Debug("ConfigurationFileBackupPath: {0}", this.ConfigurationFileBackupPath);
        }

        public string ExePath { get; set; }

        public string LogFilePath
        {
            get { return this.pathConfiguration.LogFilePath; }
        }

        public string SyncthingCustomHomePath
        {
            get { return this.pathConfiguration.SyncthingCustomHomePath; }
        }

        public string SyncthingPath
        {
            get { return this.pathConfiguration.SyncthingPath; }
        }

        public string SyncthingBackupPath
        {
            get { return Path.Combine(this.ExePath, "syncthing.exe"); }
        }

        public string ConfigurationFilePath
        {
            get { return this.pathConfiguration.ConfigurationFilePath; }
        }

        public string ConfigurationFileBackupPath
        {
            get { return this.pathConfiguration.ConfigurationFileBackupPath; }
        }

        public string UpdatesDownloadPath
        {
            get { return Path.Combine(Path.GetTempPath(), "SyncTrayzor"); }
        }

        public string InstallCountFilePath
        {
            get { return Path.Combine(this.ExePath, "InstallCount.txt"); }
        }
    }
}
