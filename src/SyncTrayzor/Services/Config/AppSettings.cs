using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace SyncTrayzor.Services.Config
{
    public class AppSettingsConfigurationHandler : ConfigurationSection
    {
        private object appSettings;

        protected override void DeserializeSection(XmlReader reader)
        {
            var serializer = new XmlSerializer(typeof(AppSettings), new XmlRootAttribute(this.SectionInformation.Name));
            this.appSettings = serializer.Deserialize(reader);
        }

        protected override object GetRuntimeObject()
        {
            return this.appSettings;
        }
    }

    public class AppSettings
    {
        public static readonly AppSettings Instance = (AppSettings)ConfigurationManager.GetSection("settings");

        public string UpdateApiUrl { get; set; } = "http://synctrayzor.antonymale.co.uk/version-check";
        public string HomepageUrl { get; set; } = "http://github.com/canton7/SyncTrayzor";
        public int DirectoryWatcherBackoffMilliseconds { get; set; } = 2000;
        public int DirectoryWatcherFolderExistenceCheckMilliseconds { get; set; } = 3000;
        public string IssuesUrl { get; set; } = "http://github.com/canton7/SyncTrayzor/issues";
        public bool EnableAutostartOnFirstStart { get; set; } = false;
        public int CefRemoteDebuggingPort { get; set; } = 0;
        public SyncTrayzorVariant Variant { get; set; } = SyncTrayzorVariant.Portable;
        public int UpdateCheckIntervalSeconds { get; set; } = 43200;
        public int SyncthingConnectTimeoutSeconds { get; set; } = 600;
        public bool EnforceSingleProcessPerUser { get; set; } = true;

        public PathConfiguration PathConfiguration { get; set; } = new PathConfiguration();

        public Configuration DefaultUserConfiguration { get; set; } = new Configuration();

        public override string ToString()
        {
            var serializer = new XmlSerializer(typeof(AppSettings));
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, this);
                return writer.ToString();
            }
        }
    }
}
