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

        public string SyncthingAlternateHomePath
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
            if (!File.Exists(Path.GetDirectoryName(this.ConfigurationFilePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(this.ConfigurationFilePath));

            if (!File.Exists(this.ConfigurationFilePath))
            {
                this.HadToCreateConfiguration = true;
                var configuration = new Configuration(this.SyncThingPath, this.GenerateApiKey(), this.IsPortableMode);
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
