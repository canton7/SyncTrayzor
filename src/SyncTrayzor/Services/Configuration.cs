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
    }


    [XmlRoot("Configuration")]
    public class Configuration
    {
        public string SyncThingPath { get; set; }
        public bool ShowTrayIconOnlyOnClose { get; set; }
        public bool CloseToTray { get; set; }
        public bool ShowSynchronizedBalloon { get; set; }
        public string SyncThingAddress { get; set; }
        public bool StartOnLogon { get; set; }
        public bool StartMinimized { get; set; }
        public bool StartSyncThingAutomatically { get; set; }
        public string SyncThingApiKey { get; set; }
        [XmlArrayItem("Folder")]
        public List<FolderConfiguration> Folders { get; set; }

        [XmlIgnore]
        public Version LatestNotifiedVersion { get; set; }
        [XmlElement("LatestNotifiedVersion")]
        public string LatestNotifiedVersionRaw
        {
            get { return this.LatestNotifiedVersion == null ? null : this.LatestNotifiedVersion.ToString(); }
            set { this.LatestNotifiedVersion = value == null ? null : new Version(value); }
        }

        public Configuration()
            : this(null, null)
        { }

        public Configuration(string syncThingPath, string syncThingApiKey)
        {
            this.SyncThingPath = syncThingPath;
            this.ShowTrayIconOnlyOnClose = false;
            this.CloseToTray = true;
            this.ShowSynchronizedBalloon = true;
            this.SyncThingAddress = "http://localhost:8384";
            this.StartOnLogon = false;
            this.StartMinimized = true;
            this.StartSyncThingAutomatically = true;
            this.SyncThingApiKey = syncThingApiKey;
            this.Folders = new List<FolderConfiguration>();
            this.LatestNotifiedVersion = null;
        }

        public Configuration(Configuration other)
        {
            this.SyncThingPath = other.SyncThingPath;
            this.ShowTrayIconOnlyOnClose = other.ShowTrayIconOnlyOnClose;
            this.CloseToTray = other.CloseToTray;
            this.ShowSynchronizedBalloon = other.ShowSynchronizedBalloon;
            this.SyncThingAddress = other.SyncThingAddress;
            this.StartOnLogon = other.StartOnLogon;
            this.StartMinimized = other.StartMinimized;
            this.StartSyncThingAutomatically = other.StartSyncThingAutomatically;
            this.SyncThingApiKey = other.SyncThingApiKey;
            this.Folders = other.Folders.Select(x => new FolderConfiguration(x)).ToList();
            this.LatestNotifiedVersion = other.LatestNotifiedVersion;
        }
    }
}
