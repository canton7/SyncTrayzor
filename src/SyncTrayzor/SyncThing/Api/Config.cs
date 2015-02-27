using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.Api
{
    public class ConfigFolderDevice
    {
        [JsonProperty("DeviceID")]
        public string DeviceId { get; set; }

        public override string ToString()
        {
            return String.Format("<Device deviceId={0}>", this.DeviceId);
        }
    }

    public class ConfigFolder
    {
        [JsonProperty("ID")]
        public string ID { get; set; }

        [JsonProperty("Path")]
        public string Path { get; set; }

        [JsonProperty("Devices")]
        public List<ConfigFolderDevice> Devices { get; set; }

        [JsonProperty("ReadOnly")]
        public bool ReadOnly { get; set; }

        [JsonProperty("RescanIntervalS")]
        public long RescanIntervalSeconds { get; set; }

        public TimeSpan RescanInterval
        {
            get { return TimeSpan.FromSeconds(this.RescanIntervalSeconds); }
            set { this.RescanIntervalSeconds = (long)value.TotalSeconds; }
        }

        public override string ToString()
        {
            return String.Format("<Folder id={0} path={1} devices=[{2}] readonly={3} rescalinterval={4}>", this.ID, this.Path, String.Join(", ", this.Devices), this.ReadOnly, this.RescanInterval);
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

        public override string ToString()
        {
            return String.Format("<Device id={0} name={1} addresses=[{2}] compression={3} certname={4} isintroducer={5}>", this.DeviceID, this.Name, String.Join(", ", this.Addresses), this.Compression, this.CertName, this.IsIntroducer);
        }
    }

    public class Config
    {
        [JsonProperty("Version")]
        public long Version { get; set; }

        [JsonProperty("Folders")]
        public List<ConfigFolder> Folders { get; set; }

        [JsonProperty("Devices")]
        public List<ConfigDevice> Devices { get; set; }

        public override string ToString()
        {
            return String.Format("<Config version={0} folders=[{1}] devices=[{2}]>", this.Version, String.Join(", ", this.Folders), String.Join(", ", this.Devices));
        }
    }
}
