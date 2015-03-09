using NLog;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SyncTrayzor.Services
{
    public class ConfigurationChangedEventArgs : EventArgs
    {
        public Configuration NewConfiguration { get; private set; }

        public ConfigurationChangedEventArgs(Configuration newConfiguration)
        {
            this.NewConfiguration = newConfiguration;
        }
    }

    public interface IConfigurationProvider
    {
        event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;

        bool HadToCreateConfiguration { get; }
        bool IsPortableMode { get; set; }
        string LogFilePath { get; }
        string SyncThingPath { get; }
        string SyncthingCustomHomePath { get; }

        void EnsureEnvironmentConsistency();
        Configuration Load();
        void Save(Configuration config);
    }

    public class ConfigurationProvider : IConfigurationProvider
    {
        private const string apiKeyChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-";
        private const int apiKeyLength = 40;

        private readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly SynchronizedEventDispatcher eventDispatcher;
        private readonly XmlSerializer serializer = new XmlSerializer(typeof(Configuration));

        private readonly object currentConfigLock = new object();
        private Configuration currentConfig;

        public event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;

        public bool HadToCreateConfiguration { get; private set; }
        public bool IsPortableMode { get; set; }

        public string ExePath
        {
            get { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); }
        }

        public string RoamingPath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SyncTrayzor"); }
        }

        public string LocalPath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SyncTrayzor"); }
        }

        public string LogFilePath
        {
            get { return this.IsPortableMode ? Path.Combine(this.ExePath, "logs") : Path.Combine(this.RoamingPath); }
        }

        public string SyncthingCustomHomePath
        {
            get { return this.IsPortableMode ? Path.Combine(this.ExePath, "data", "syncthing") : Path.Combine(this.LocalPath, "syncthing"); }
        }
        
        public string SyncThingPath
        {
            get { return this.IsPortableMode ? Path.Combine(this.ExePath, "syncthing.exe") : Path.Combine(this.RoamingPath, "syncthing.exe"); }
        }

        public string SyncThingBackupPath
        {
            get { return Path.Combine(this.ExePath, "syncthing.exe"); }
        }

        public string ConfigurationFilePath
        {
            get { return this.IsPortableMode ? Path.Combine(this.ExePath, "data", "config.xml") : Path.Combine(this.RoamingPath, "config.xml"); }
        }

        public ConfigurationProvider()
        {
            this.eventDispatcher = new SynchronizedEventDispatcher(this);
        }

        public void EnsureEnvironmentConsistency()
        {
            logger.Debug("IsPortableMode: {0}", this.IsPortableMode);
            logger.Debug("ExePath: {0}", this.ExePath);
            logger.Debug("LogFilePath: {0}", this.LogFilePath);
            logger.Debug("SyncthingCustomHomePath: {0}", this.LogFilePath);
            logger.Debug("SyncThingPath: {0}", this.SyncThingPath);
            logger.Debug("SyncThingBackupPath: {0}", this.SyncThingBackupPath);
            logger.Debug("ConfigurationFilePath: {0}", this.ConfigurationFilePath);

            if (!File.Exists(Path.GetDirectoryName(this.ConfigurationFilePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(this.ConfigurationFilePath));

            if (!File.Exists(this.SyncThingPath))
            {
                if (File.Exists(this.SyncThingBackupPath))
                {
                    logger.Info("Syncthing doesn't exist at {0}, so copying from {1}", this.SyncThingPath, this.SyncThingBackupPath);
                    File.Copy(this.SyncThingBackupPath, this.SyncThingPath);
                }
                else
                    throw new Exception(String.Format("Unable to find Syncthing at {0} or {1}", this.SyncThingPath, this.SyncThingBackupPath));
            }
            else if (this.SyncThingPath != this.SyncThingBackupPath && File.Exists(this.SyncThingBackupPath) &&
                File.GetLastWriteTimeUtc(this.SyncThingPath) < File.GetLastWriteTimeUtc(this.SyncThingBackupPath))
            {
                logger.Info("Syncthing at {0} is older ({1}) than at {2} ({3}, so overwriting from backup",
                    this.SyncThingPath, File.GetLastWriteTimeUtc(this.SyncThingPath), this.SyncThingBackupPath, File.GetLastWriteTimeUtc(this.SyncThingBackupPath));
                File.Copy(this.SyncThingBackupPath, this.SyncThingPath, true);
            }

            if (!File.Exists(this.ConfigurationFilePath))
            {
                logger.Info("Configuration file {0} doesn't exist, so creating", this.ConfigurationFilePath);
                this.HadToCreateConfiguration = true;
                var configuration = new Configuration(this.GenerateApiKey(), this.IsPortableMode);
                this.Save(configuration);
            }
        }

        public Configuration Load()
        {
            lock (this.currentConfigLock)
            {
                if (this.currentConfig == null)
                    this.currentConfig = this.LoadFromDisk();

                return new Configuration(this.currentConfig);
            }
        }

        public void Save(Configuration config)
        {
            logger.Debug("Saving configuration: {0}", config);
            lock (this.currentConfigLock)
            {
                this.currentConfig = config;
                using (var stream = File.Open(this.ConfigurationFilePath, FileMode.Create))
                {
                    this.serializer.Serialize(stream, config);
                }
            }
            this.OnConfigurationChanged(config);
        }

        private Configuration LoadFromDisk()
        {
            using (var stream = File.OpenRead(this.ConfigurationFilePath))
            {
                var config = (Configuration)this.serializer.Deserialize(stream);
                logger.Info("Loaded configuration: {0}", config);
                return config;
            }
        }

        private string GenerateApiKey()
        {
            var random = new Random();
            var apiKey = new char[apiKeyLength];
            for (int i = 0; i < apiKeyLength; i++)
            {
                apiKey[i] = apiKeyChars[random.Next(apiKeyChars.Length)];
            }
            return new string(apiKey);
        }

        private void OnConfigurationChanged(Configuration newConfiguration)
        {
            this.eventDispatcher.Raise(this.ConfigurationChanged, new ConfigurationChangedEventArgs(newConfiguration));
        }
    }

    public class ConfigurationException : Exception
    {
        public ConfigurationException(string message) : base(message)
        { }
    }
}
