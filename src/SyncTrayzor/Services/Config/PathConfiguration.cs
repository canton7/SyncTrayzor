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
        public string SyncthingCustomHomePath { get; set; }
        public string SyncthingPath { get; set; }
        public string ConfigurationFilePath { get; set; }

        public PathConfiguration()
        {
            this.LogFilePath = @"%EXEPATH%\logs";
            this.SyncthingCustomHomePath = @"%EXEPATH%\data\syncthing";
            this.SyncthingPath = @"%EXEPATH%\syncthing.exe";
            this.ConfigurationFilePath = @"%EXEPATH%\data\config.xml";
        }

        public void Transform(Func<string, string> transfomer)
        {
            this.LogFilePath = transfomer(this.LogFilePath);
            this.SyncthingCustomHomePath = transfomer(this.SyncthingCustomHomePath);
            this.SyncthingPath = transfomer(this.SyncthingPath);
            this.ConfigurationFilePath = transfomer(this.ConfigurationFilePath);
        }
    }
}
