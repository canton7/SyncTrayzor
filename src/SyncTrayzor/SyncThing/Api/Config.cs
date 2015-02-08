using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.Api
{
    public class ConfigFolder
    {
        [JsonProperty("ID")]
        public string ID { get; set; }

        [JsonProperty("Path")]
        public string Path { get; set; }

        [JsonProperty("Devices")]
        public List<ConfigDevice> Devices { get; set; }

        [JsonProperty("ReadOnly")]
        public bool ReadOnly { get; set; }

        [JsonProperty("RescanIntervalS")]
        public int RescanIntervalSeconds { get; set; }

        public TimeSpan RescanInterval
        {
            get { return TimeSpan.FromSeconds(this.RescanIntervalSeconds); }
            set { this.RescanIntervalSeconds = (int)value.TotalSeconds; }
        }
    }

    public class ConfigDevice
    {
        [JsonProperty("DeviceID")]
        public string DeviceID { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Addresses")]
        public List<string> Addresses { get; set; }

        [JsonProperty("Compression")]
        public bool Compression { get; set; }

        [JsonProperty("CertName")]
        public string CertName { get; set; }

        [JsonProperty("Introducer")]
        public bool IsIntroducer { get; set; }
    }

    public class Config
    {
        [JsonProperty("Version")]
        public int Version { get; set; }

        [JsonProperty("Folders")]
        public List<ConfigFolder> Folders { get; set; }

        [JsonProperty("Deviecs")]
        public List<ConfigDevice> Devices { get; set; }
    }
}
