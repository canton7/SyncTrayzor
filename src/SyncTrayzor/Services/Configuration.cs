using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SyncTrayzor.Services
{
    [XmlRoot("Configuration")]
    public class Configuration
    {
        public bool ShowTrayIconOnlyOnClose { get; set; }
        public bool CloseToTray { get; set; }
        public string SyncThingAddress { get; set; }

        public Configuration()
        {
            this.ShowTrayIconOnlyOnClose = false;
            this.CloseToTray = false;
            this.SyncThingAddress = "http://localhost:4567";
        }

        public Configuration Clone()
        {
            return new Configuration()
            {
                ShowTrayIconOnlyOnClose = this.ShowTrayIconOnlyOnClose,
                CloseToTray = this.CloseToTray,
                SyncThingAddress = this.SyncThingAddress,
            };
        }
    }
}
