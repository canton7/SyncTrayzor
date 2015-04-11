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

namespace SyncTrayzor.Services.Config
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

        void Initialize(Configuration defaultConfiguration);
        Configuration Load();
        void Save(Configuration config);
        void AtomicLoadAndSave(Action<Configuration> setter);
    }

    public class ConfigurationProvider : IConfigurationProvider
    {
        private const string apiKeyChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-";
        private const int apiKeyLength = 40;

        private readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly SynchronizedEventDispatcher eventDispatcher;
        private readonly XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
        private readonly IApplicationPathsProvider paths;

        private readonly object currentConfigLock = new object();
        private Configuration currentConfig;

        public event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;

        public bool HadToCreateConfiguration { get; private set; }

        public ConfigurationProvider(IApplicationPathsProvider paths)
        {
            this.paths = paths;
            this.eventDispatcher = new SynchronizedEventDispatcher(this);
        }

        public void Initialize(Configuration defaultConfiguration)
        {
            if (defaultConfiguration == null)
                throw new ArgumentNullException("defaultConfiguration");

            if (!File.Exists(Path.GetDirectoryName(this.paths.ConfigurationFilePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(this.paths.ConfigurationFilePath));

            if (!File.Exists(this.paths.SyncthingPath))
            {
                if (File.Exists(this.paths.SyncthingBackupPath))
                {
                    logger.Info("Syncthing doesn't exist at {0}, so copying from {1}", this.paths.SyncthingPath, this.paths.SyncthingBackupPath);
                    File.Copy(this.paths.SyncthingBackupPath, this.paths.SyncthingPath);
                }
                else
                    throw new Exception(String.Format("Unable to find Syncthing at {0} or {1}", this.paths.SyncthingPath, this.paths.SyncthingBackupPath));
            }
            else if (this.paths.SyncthingPath != this.paths.SyncthingBackupPath && File.Exists(this.paths.SyncthingBackupPath) &&
                File.GetLastWriteTimeUtc(this.paths.SyncthingPath) < File.GetLastWriteTimeUtc(this.paths.SyncthingBackupPath))
            {
                logger.Info("Syncthing at {0} is older ({1}) than at {2} ({3}, so overwriting from backup",
                    this.paths.SyncthingPath, File.GetLastWriteTimeUtc(this.paths.SyncthingPath), this.paths.SyncthingBackupPath, File.GetLastWriteTimeUtc(this.paths.SyncthingBackupPath));
                File.Copy(this.paths.SyncthingBackupPath, this.paths.SyncthingPath, true);
            }

            this.currentConfig = this.LoadFromDisk(defaultConfiguration);
        }

        private Configuration LoadFromDisk(Configuration defaultConfiguration)
        {
            // Merge any updates from app.config / Configuration into the configuration file on disk
            // (creating if necessary)
            logger.Debug("Loaded default configuration: {0}", defaultConfiguration);
            XDocument defaultConfig;
            using (var ms = new MemoryStream())
            {
                this.serializer.Serialize(ms, defaultConfiguration);
                ms.Position = 0;
                defaultConfig = XDocument.Load(ms);
            }

            if (File.Exists(this.paths.ConfigurationFilePath))
            {
                logger.Debug("Found existing configuration at {0}", this.paths.ConfigurationFilePath);
                var loadedConfig = XDocument.Load(this.paths.ConfigurationFilePath);
                var merged = loadedConfig.Root.Elements().Union(defaultConfig.Root.Elements(), new XmlNodeComparer());
                loadedConfig.Root.ReplaceNodes(merged);
                loadedConfig.Save(this.paths.ConfigurationFilePath);
            }
            else
            {
                defaultConfig.Save(this.paths.ConfigurationFilePath);
            }
            
            Configuration configuration;
            using (var stream = File.OpenRead(this.paths.ConfigurationFilePath))
            {
                configuration = (Configuration)this.serializer.Deserialize(stream);
                logger.Info("Loaded configuration: {0}", configuration);
            }

            this.MigrateConfiguration(configuration);

            return configuration;
        }

        private void MigrateConfiguration(Configuration configuration)
        {
            bool altered = false;

            if (configuration.SyncthingApiKey == null)
            {
                configuration.SyncthingApiKey = this.GenerateApiKey();
                altered = true;
            }

            // We used to store http/https in the config, but we no longer do. A migration is necessary
            if (configuration.SyncthingAddress.StartsWith("http://"))
            {
                configuration.SyncthingAddress = configuration.SyncthingAddress.Substring("http://".Length);
                altered = true;
            }

            if (configuration.SyncthingAddress.StartsWith("https://"))
            {
                configuration.SyncthingAddress = configuration.SyncthingAddress.Substring("https://".Length);
                altered = true;
            }

            if (altered)
                this.SaveToFile(configuration);
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

        public void AtomicLoadAndSave(Action<Configuration> setter)
        {
            // We can just let them modify the current config here - since it's all inside the lock
            Configuration newConfig;
            lock (this.currentConfigLock)
            {
                setter(this.currentConfig);
                this.SaveToFile(this.currentConfig);
                newConfig = this.currentConfig;
            }
            this.OnConfigurationChanged(newConfig);
        }

        private void SaveToFile(Configuration config)
        {
            using (var stream = File.Open(this.paths.ConfigurationFilePath, FileMode.Create))
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
