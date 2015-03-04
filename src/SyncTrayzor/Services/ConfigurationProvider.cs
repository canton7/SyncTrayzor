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

        string RoamingPath { get; }
        string SyncthingAlternateHomePath { get; }

        void EnsureEnvironmentConsistency();
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

        public string ExePath
        {
            get { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); }
        }

        public string RoamingPath
        {
#if DEBUG
            get { return this.ExePath; }
#else
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SyncTrayzor"); }
#endif
        }

        public string LocalPath
        {
#if DEBUG
            get { return this.ExePath; }
#else
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SyncTrayzor"); }
#endif
        }

        public string SyncthingAlternateHomePath
        {
            get { return Path.Combine(this.LocalPath, "syncthing-home"); }
        }
        
        public string SyncThingPath
        {
            get { return Path.Combine(this.RoamingPath, "syncthing.exe"); }
        }

        public string SyncThingBackupPath
        {
            get { return Path.Combine(this.ExePath, "syncthing.exe"); }
        }

        public string ConfigurationFilePath
        {
            get { return Path.Combine(this.RoamingPath, "config.xml"); }
        }

        public ConfigurationProvider()
        {
            this.eventDispatcher = new SynchronizedEventDispatcher(this);
        }

        public void EnsureEnvironmentConsistency()
        {
            if (!String.IsNullOrWhiteSpace(this.RoamingPath))
                Directory.CreateDirectory(this.RoamingPath);

            if (!File.Exists(this.ConfigurationFilePath))
            {
                var configuration = new Configuration(this.SyncThingPath, this.GenerateApiKey());
                this.Save(configuration);
            }

            if (!File.Exists(this.SyncThingPath) && File.Exists(this.SyncThingBackupPath))
                File.Copy(this.SyncThingBackupPath, this.SyncThingPath);
        }

        public Configuration Load()
        {
            if (this.currentConfig == null)
                this.currentConfig = this.LoadFromDisk();

            return new Configuration(this.currentConfig);
        }

        public void Save(Configuration config)
        {
            this.EnsureConfigurationFileConsistency(config);
            this.currentConfig = config;
            this.OnConfigurationChanged(config);
            using (var stream = File.Open(this.ConfigurationFilePath, FileMode.Create))
            {
                this.serializer.Serialize(stream, config);
            }
        }

        private Configuration LoadFromDisk()
        {
            using (var stream = File.OpenRead(this.ConfigurationFilePath))
            {
                return (Configuration)this.serializer.Deserialize(stream);
            }
        }

        private void EnsureConfigurationFileConsistency(Configuration configuration)
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
