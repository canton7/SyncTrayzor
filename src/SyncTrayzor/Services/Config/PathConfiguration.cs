using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public PathConfiguration()
        {
            this.LogFilePath = @"%EXEPATH%\logs";
            this.ConfigurationFilePath = @"%EXEPATH%\data\config.xml";
            this.ConfigurationFileBackupPath = @"%EXEPATH%\data\config-backups";
            this.CefCachePath = @"%EXEPATH%\data\cef\cache";
        }
    }
}
