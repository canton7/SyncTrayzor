using Newtonsoft.Json;

namespace SyncTrayzor.Syncthing.ApiClient
{
    public class SyncthingVersion
    {
        [JsonProperty("arch")]
        public string Arch { get; set; }

        [JsonProperty("longVersion")]
        public string LongVersion { get; set; }

        [JsonProperty("os")]
        public string OS { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        public override string ToString()
        {
            return $"<Version arch={this.Arch} longVersion={this.LongVersion} os={this.OS} version={this.Version}>";
        }
    }
}
