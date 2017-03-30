using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SyncTrayzor.Syncthing.ApiClient
{
    public class ConfigFolderDevice : IEquatable<ConfigFolderDevice>
    {
        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        public bool Equals(ConfigFolderDevice other)
        {
            return other != null && this.DeviceId == other.DeviceId;
        }

        public override string ToString()
        {
            return $"<Device deviceId={this.DeviceId}>";
        }
    }

    public class ConfigFolder : IEquatable<ConfigFolder>
    {
        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("devices")]
        public List<ConfigFolderDevice> Devices { get; set; }

        // This has changed type, and we don't use it anyway
        //[JsonProperty("type")]
        //public bool Type { get; set; }

        [JsonProperty("rescanIntervalS")]
        public long RescanIntervalSeconds { get; set; }

        public TimeSpan RescanInterval
        {
            get => TimeSpan.FromSeconds(this.RescanIntervalSeconds);
            set => this.RescanIntervalSeconds = (long)value.TotalSeconds;
        }

        [JsonProperty("invalid")]
        public string Invalid { get; set; }

        public bool Equals(ConfigFolder other)
        {
            return other != null &&
                this.ID == other.ID &&
                this.Path == other.Path &&
                this.Devices.SequenceEqual(other.Devices) &&
                //this.Type == other.Type &&
                this.RescanIntervalSeconds == other.RescanIntervalSeconds &&
                this.Invalid == other.Invalid;
        }

        public override string ToString()
        {
            return $"<Folder id={this.ID} label={this.Label} path={this.Path} devices=[{String.Join(", ", this.Devices)}] rescalinterval={this.RescanInterval} invalid={this.Invalid}>";
        }
    }

    public class ConfigDevice : IEquatable<ConfigDevice>
    {
        [JsonProperty("deviceID")]
        public string DeviceID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("addresses")]
        public List<string> Addresses { get; set; }

        // Apparently this can be 'never'
        // We don't use it, so commenting until it decided what data type it wants to be
        //[JsonProperty("compression")]
        //public string Compression { get; set; }

        [JsonProperty("certName")]
        public string CertName { get; set; }

        [JsonProperty("introducer")]
        public bool IsIntroducer { get; set; }

        public bool Equals(ConfigDevice other)
        {
            return other != null &&
                this.DeviceID == other.DeviceID &&
                this.Name == other.Name &&
                this.Addresses.SequenceEqual(other.Addresses) &&
                this.CertName == other.CertName &&
                this.IsIntroducer == other.IsIntroducer;
        }

        public override string ToString()
        {
            return $"Device id={this.DeviceID} name={this.Name} addresses=[{String.Join(", ", this.Addresses)}] compression=N/A certname={this.CertName} isintroducer={this.IsIntroducer}>";
        }
    }

    public class Config : IEquatable<Config>
    {
        [JsonProperty("version")]
        public long Version { get; set; }

        [JsonProperty("folders")]
        public List<ConfigFolder> Folders { get; set; }

        [JsonProperty("devices")]
        public List<ConfigDevice> Devices { get; set; }

        public bool Equals(Config other)
        {
            return other != null &&
                this.Version == other.Version &&
                this.Folders.SequenceEqual(other.Folders) &&
                this.Devices.SequenceEqual(other.Devices);
        }

        public override string ToString()
        {
            return $"<Config version={this.Version} folders=[{String.Join(", ", this.Folders)}] devices=[{String.Join(", ", this.Devices)}]>";
        }
    }
}
