using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        Configuration Load();
        void Save(Configuration config);
    }

    public class ConfigurationProvider : IConfigurationProvider
    {
        private readonly SynchronizedEventDispatcher eventDispatcher;
        private readonly XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
        private Configuration currentConfig;

        public event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;

        public string BasePath
        {
#if NDEBUG
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SyncTrayzor"); }
#else
            get { return "config"; }
#endif
        }

        public string ConfigurationFilePath
        {
            get { return Path.Combine(this.BasePath, "config.xml"); }
        }

        public ConfigurationProvider()
        {
            this.eventDispatcher = new SynchronizedEventDispatcher(this);
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
            this.currentConfig = config;
            this.OnConfigurationChanged(config);
            using (var stream = File.Open(this.ConfigurationFilePath, FileMode.Create))
            {
                this.serializer.Serialize(stream, config);
            }
        }

        private Configuration LoadFromDisk()
        {
            if (!File.Exists(this.ConfigurationFilePath))
                return new Configuration();

            using (var stream = File.OpenRead(this.ConfigurationFilePath))
            {
                return (Configuration)this.serializer.Deserialize(stream);
            }
        }

        private void OnConfigurationChanged(Configuration newConfiguration)
        {
            this.eventDispatcher.Raise(this.ConfigurationChanged, new ConfigurationChangedEventArgs(newConfiguration));
        }
    }
}
