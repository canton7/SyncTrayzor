using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SyncTrayzor.SyncThing.ApiClient
{
    public class ConfigFolderDevice
    {
        [JsonProperty("DeviceID")]
        public string DeviceId { get; set; }

        public override string ToString()
        {
            return $"<Device deviceId={this.DeviceId}>";
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

        [JsonProperty("invalid")]
        public string Invalid { get; set; }

        public override string ToString()
        {
            return $"<Folder id={this.ID} path={this.Path} devices=[{String.Join(", ", this.Devices)}] readonly={this.ReadOnly} rescalinterval={this.RescanInterval} invalid={this.Invalid}>";
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

        // Apparently this can be 'never'
        // We don't use it, so commenting until it decided what data type it wants to be
        //[JsonProperty("Compression")]
        //public bool Compression { get; set; }

        [JsonProperty("CertName")]
        public string CertName { get; set; }

        [JsonProperty("Introducer")]
        public bool IsIntroducer { get; set; }

        public override string ToString()
        {
            return $"Device id={this.DeviceID} name={this.Name} addresses=[{String.Join(", ", this.Addresses)}] compression=N/A certname={this.CertName} isintroducer={this.IsIntroducer}>";
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
            return $"<Config version={this.Version} folders=[{String.Join(", ", this.Folders)}] devices=[{String.Join(", ", this.Devices)}]>";
        }
    }
}
