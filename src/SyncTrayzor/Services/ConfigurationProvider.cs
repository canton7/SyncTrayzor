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

        string BasePath { get; }

        Configuration Load();
        void Save(Configuration config);
    }

    public class ConfigurationProvider : IConfigurationProvider
    {
        private const string apiKeyChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int apiKeyLength = 40;

        private readonly SynchronizedEventDispatcher eventDispatcher;
        private readonly XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
        private Configuration currentConfig;

        public event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;

        public string BasePath
        {
#if DEBUG
            get { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); }
#else
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SyncTrayzor"); }
#endif
        }

        public string ConfigurationFilePath
        {
            get { return Path.Combine(this.BasePath, "config.xml"); }
        }

        public ConfigurationProvider()
        {
            this.eventDispatcher = new SynchronizedEventDispatcher(this);
            if (!String.IsNullOrWhiteSpace(this.BasePath))
                Directory.CreateDirectory(this.BasePath);
        }

        public Configuration Load()
        {
            if (this.currentConfig == null)
                this.currentConfig = this.LoadFromDisk();

            return new Configuration(this.currentConfig);
        }

        public void Save(Configuration config)
        {
            this.EnsureConsistency(config);
            this.currentConfig = config;
            this.OnConfigurationChanged(config);
            using (var stream = File.Open(this.ConfigurationFilePath, FileMode.Create))
            {
                this.serializer.Serialize(stream, config);
            }
        }

        private Configuration LoadFromDisk()
        {
            Configuration configuration;

            if (!File.Exists(this.ConfigurationFilePath))
            {
                configuration = new Configuration(Path.Combine(this.BasePath, "syncthing.exe"), this.GenerateApiKey());
            }
            else
            {
                using (var stream = File.OpenRead(this.ConfigurationFilePath))
                {
                    configuration = (Configuration)this.serializer.Deserialize(stream);
                }
            }

            return configuration;
        }

        private void EnsureConsistency(Configuration configuration)
        {
            if (!File.Exists(configuration.SyncthingPath))
                throw new ConfigurationException(String.Format("Unable to find file {0}", configuration.SyncthingPath));
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
