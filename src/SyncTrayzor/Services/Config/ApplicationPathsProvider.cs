using NLog;
using SyncTrayzor.Utils;
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
            logger.Debug("SyncThingBackupPath: {0}", this.SyncthingBackupPath);
            logger.Debug("ConfigurationFilePath: {0}", this.ConfigurationFilePath);
            logger.Debug("ConfigurationFileBackupPath: {0}", this.ConfigurationFileBackupPath);
            logger.Debug("CefCachePath: {0}", this.CefCachePath);
        }

        public string ExePath { get; set; }

        public string LogFilePath
        {
            get { return EnvVarTransformer.Transform(this.pathConfiguration.LogFilePath); }
        }

        public string SyncthingBackupPath
        {
            get { return Path.Combine(this.ExePath, "syncthing.exe"); }
        }

        public string ConfigurationFilePath
        {
            get { return EnvVarTransformer.Transform(this.pathConfiguration.ConfigurationFilePath); }
        }

        public string ConfigurationFileBackupPath
        {
            get { return EnvVarTransformer.Transform(this.pathConfiguration.ConfigurationFileBackupPath); }
        }

        public string CefCachePath
        {
            get { return EnvVarTransformer.Transform(this.pathConfiguration.CefCachePath); }
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
