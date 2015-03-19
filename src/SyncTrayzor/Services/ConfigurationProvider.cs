using NLog;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace SyncTrayzor.Services
{
    public class ConfigurationChangedEventArgs : EventArgs
    {
        private readonly Configuration baseConfiguration;
        public Configuration NewConfiguration
        {
            // Ensure we always clone it, so people can modify
            get { return new Configuration(this.baseConfiguration); }
        }

        public ConfigurationChangedEventArgs(Configuration newConfiguration)
        {
            this.baseConfiguration = newConfiguration;
        }
    }

    public interface IConfigurationProvider
    {
        event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;

        bool HadToCreateConfiguration { get; }
        string LogFilePath { get; }
        string SyncthingPath { get; }
        string SyncthingCustomHomePath { get; }

        void Initialize(PathConfiguration pathConfiguration, Configuration defaultConfiguration);
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
        private PathConfiguration pathConfiguration;

        private readonly object currentConfigLock = new object();
        private Configuration currentConfig;

        public event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;

        public bool HadToCreateConfiguration { get; private set; }

        public string ExePath
        {
            get { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); }
        }

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

        public ConfigurationProvider()
        {
            this.eventDispatcher = new SynchronizedEventDispatcher(this);
        }

        public void Initialize(PathConfiguration pathConfiguration, Configuration defaultConfiguration)
        {
            this.pathConfiguration = pathConfiguration;

            logger.Debug("ExePath: {0}", this.ExePath);
            logger.Debug("LogFilePath: {0}", this.LogFilePath);
            logger.Debug("SyncthingCustomHomePath: {0}", this.LogFilePath);
            logger.Debug("SyncThingPath: {0}", this.SyncthingPath);
            logger.Debug("SyncThingBackupPath: {0}", this.SyncthingBackupPath);
            logger.Debug("ConfigurationFilePath: {0}", this.ConfigurationFilePath);

            if (!File.Exists(Path.GetDirectoryName(this.ConfigurationFilePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(this.ConfigurationFilePath));

            if (!File.Exists(this.SyncthingPath))
            {
                if (File.Exists(this.SyncthingBackupPath))
                {
                    logger.Info("Syncthing doesn't exist at {0}, so copying from {1}", this.SyncthingPath, this.SyncthingBackupPath);
                    File.Copy(this.SyncthingBackupPath, this.SyncthingPath);
                }
                else
                    throw new Exception(String.Format("Unable to find Syncthing at {0} or {1}", this.SyncthingPath, this.SyncthingBackupPath));
            }
            else if (this.SyncthingPath != this.SyncthingBackupPath && File.Exists(this.SyncthingBackupPath) &&
                File.GetLastWriteTimeUtc(this.SyncthingPath) < File.GetLastWriteTimeUtc(this.SyncthingBackupPath))
            {
                logger.Info("Syncthing at {0} is older ({1}) than at {2} ({3}, so overwriting from backup",
                    this.SyncthingPath, File.GetLastWriteTimeUtc(this.SyncthingPath), this.SyncthingBackupPath, File.GetLastWriteTimeUtc(this.SyncthingBackupPath));
                File.Copy(this.SyncthingBackupPath, this.SyncthingPath, true);
            }

            this.currentConfig = this.LoadFromDisk(defaultConfiguration);
        }

        private Configuration LoadFromDisk(Configuration defaultConfiguration)
        {
            // Merge any updates from app.config / Configuration into the configuration file on disk
            // (creating if necessary)
            defaultConfiguration = defaultConfiguration ?? new Configuration();
            logger.Debug("Loaded default configuration: {0}", defaultConfiguration);
            XDocument defaultConfig;
            using (var ms = new MemoryStream())
            {
                this.serializer.Serialize(ms, defaultConfiguration);
                ms.Position = 0;
                defaultConfig = XDocument.Load(ms);
            }

            if (File.Exists(this.ConfigurationFilePath))
            {
                logger.Debug("Found existing configuration at {0}", this.ConfigurationFilePath);
                var loadedConfig = XDocument.Load(this.ConfigurationFilePath);
                var merged = loadedConfig.Root.Elements().Union(defaultConfig.Root.Elements(), new XmlNodeComparer());
                loadedConfig.Root.ReplaceNodes(merged);
                loadedConfig.Save(this.ConfigurationFilePath);
            }
            else
            {
                defaultConfig.Save(this.ConfigurationFilePath);
            }
            
            Configuration configuration;
            using (var stream = File.OpenRead(this.ConfigurationFilePath))
            {
                configuration = (Configuration)this.serializer.Deserialize(stream);
                logger.Info("Loaded configuration: {0}", configuration);
            }

            if (configuration.SyncthingApiKey == null)
            {
                configuration.SyncthingApiKey = this.GenerateApiKey();
                this.SaveToFile(configuration);
            }

            return configuration;
        }

        public Configuration Load()
        {
            lock (this.currentConfigLock)
            {
                return new Configuration(this.currentConfig);
            }
        }

        public void Save(Configuration config)
        {
            logger.Debug("Saving configuration: {0}", config);
            lock (this.currentConfigLock)
            {
                this.currentConfig = config;
                this.SaveToFile(config);
            }
            this.OnConfigurationChanged(config);
        }

        private void SaveToFile(Configuration config)
        {
            using (var stream = File.Open(this.ConfigurationFilePath, FileMode.Create))
            {
                this.serializer.Serialize(stream, config);
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

        private class XmlNodeComparer : IEqualityComparer<XElement>
        {
            public bool Equals(XElement x, XElement y)
            {
                return x.Name == y.Name;
            }

            public int GetHashCode(XElement obj)
            {
                return obj.Name.GetHashCode();
            }
        }
    }

    public class ConfigurationException : Exception
    {
        public ConfigurationException(string message) : base(message)
        { }
    }
}
