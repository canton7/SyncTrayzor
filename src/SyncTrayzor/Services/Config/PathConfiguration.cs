using System.Xml.Serialization;

namespace SyncTrayzor.Services.Config
{
    [XmlRoot("PathConfiguration")]
    public class PathConfiguration
    {
        public string LogFilePath { get; set; }
        public string ConfigurationFilePath { get; set; }
        public string ConfigurationFileBackupPath { get; set; }
        public string CefCachePath { get; set; }
        public string SyncthingPath { get; set; }
        public string SyncthingHomePath { get; set; }

        public PathConfiguration()
        {
            this.LogFilePath = @"logs";
            this.ConfigurationFilePath = @"data\config.xml";
            this.ConfigurationFileBackupPath = @"data\config-backups";
            this.CefCachePath = @"data\cef\cache";
            this.SyncthingPath = @"data\syncthing.exe";
            this.SyncthingHomePath = @"data\syncthing";
        }
    }
}
