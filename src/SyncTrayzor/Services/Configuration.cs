using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SyncTrayzor.Services
{
    public class FolderConfiguration
    {
        public string ID { get; set; }
        public bool IsWatched { get; set; }

        public FolderConfiguration()
        { }

        public FolderConfiguration(string id, bool isWatched)
        {
            this.ID = id;
            this.IsWatched = isWatched;
        }

        public FolderConfiguration(FolderConfiguration other)
        {
            this.ID = other.ID;
            this.IsWatched = other.IsWatched;
        }

        public override string ToString()
        {
            return String.Format("<Folder ID={0} IsWatched={1}>", this.ID, this.IsWatched);
        }
    }


    [XmlRoot("Configuration")]
    public class Configuration
    {
        public bool ShowTrayIconOnlyOnClose { get; set; }
        public bool MinimizeToTray { get; set; }
        public bool CloseToTray { get; set; }
        public bool ShowSynchronizedBalloon { get; set; }
        public bool ShowDeviceConnectivityBalloons { get; set; }
        public string SyncthingAddress { get; set; }
        public bool StartSyncthingAutomatically { get; set; }
        public string SyncthingApiKey { get; set; }
        public string SyncthingTraceFacilities { get; set; }
        public bool SyncthingUseCustomHome { get; set; }
        [XmlArrayItem("Folder")]
        public List<FolderConfiguration> Folders { get; set; }
        public bool NotifyOfNewVersions { get; set; }
        public bool ObfuscateDeviceIDs { get; set; }

        [XmlIgnore]
        public Version LatestNotifiedVersion { get; set; }
        [XmlElement("LatestNotifiedVersion")]
        public string LatestNotifiedVersionRaw
        {
            get { return this.LatestNotifiedVersion == null ? null : this.LatestNotifiedVersion.ToString(); }
            set { this.LatestNotifiedVersion = value == null ? null : new Version(value); }
        }

        public Configuration()
            : this(null, false)
        { }

        public Configuration(string syncThingApiKey, bool isPortableMode)
        {
            this.ShowTrayIconOnlyOnClose = false;
            this.MinimizeToTray = false;
            this.CloseToTray = true;
            this.ShowSynchronizedBalloon = true;
            this.ShowDeviceConnectivityBalloons = true;
            this.SyncthingAddress = "http://localhost:8384";
            this.StartSyncthingAutomatically = true;
            this.SyncthingApiKey = syncThingApiKey;
            this.SyncthingTraceFacilities = null;
            this.SyncthingUseCustomHome = isPortableMode;
            this.Folders = new List<FolderConfiguration>();
            this.NotifyOfNewVersions = true;
            this.ObfuscateDeviceIDs = true;
            this.LatestNotifiedVersion = null;
        }

        public Configuration(Configuration other)
        {
            this.ShowTrayIconOnlyOnClose = other.ShowTrayIconOnlyOnClose;
            this.MinimizeToTray = other.MinimizeToTray;
            this.CloseToTray = other.CloseToTray;
            this.ShowSynchronizedBalloon = other.ShowSynchronizedBalloon;
            this.ShowDeviceConnectivityBalloons = other.ShowDeviceConnectivityBalloons;
            this.SyncthingAddress = other.SyncthingAddress;
            this.StartSyncthingAutomatically = other.StartSyncthingAutomatically;
            this.SyncthingApiKey = other.SyncthingApiKey;
            this.SyncthingTraceFacilities = other.SyncthingTraceFacilities;
            this.SyncthingUseCustomHome = other.SyncthingUseCustomHome;
            this.Folders = other.Folders.Select(x => new FolderConfiguration(x)).ToList();
            this.NotifyOfNewVersions = other.NotifyOfNewVersions;
            this.ObfuscateDeviceIDs = other.ObfuscateDeviceIDs;
            this.LatestNotifiedVersion = other.LatestNotifiedVersion;
        }

        public override string ToString()
        {
            return String.Format("<Configuration ShowTrayIconOnlyOnClose={0} MinimizeToTray={1} CloseToTray={2} ShowSynchronizedBalloon={3} " +
                "ShowDeviceConnectivityBalloons={4} SyncthingAddress={5} StartSyncthingAutomatically={6} SyncthingApiKey={7} SyncthingTraceFacilities={8} " +
                "SyncthingUseCustomHome={9} Folders=[{10}] NotifyOfNewVersions={11} LastNotifiedVersion={12} ObfuscateDeviceIDs={13}>",
                this.ShowTrayIconOnlyOnClose, this.MinimizeToTray, this.CloseToTray, this.ShowSynchronizedBalloon, this.ShowDeviceConnectivityBalloons,
                this.SyncthingAddress, this.StartSyncthingAutomatically, this.SyncthingApiKey, this.SyncthingTraceFacilities,
                this.SyncthingUseCustomHome, String.Join(", ", this.Folders), this.NotifyOfNewVersions, this.LatestNotifiedVersion,
                this.ObfuscateDeviceIDs);
        }
    }
}
