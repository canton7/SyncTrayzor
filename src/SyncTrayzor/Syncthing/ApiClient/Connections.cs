using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SyncTrayzor.Syncthing.ApiClient
{
    public class ItemConnectionData
    {
        [JsonProperty("at")]
        public DateTime At { get; set; }

        [JsonProperty("inBytesTotal")]
        public long InBytesTotal { get; set; }

        [JsonProperty("outBytesTotal")]
        public long OutBytesTotal { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("clientVersion")]
        public string ClientVersion { get; set; }

        [JsonProperty("connected")]
        public string Conneted { get; set; }

        [JsonProperty("paused")]
        public bool Paused { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public class Connections
    {
        [JsonProperty("total")]
        public ItemConnectionData Total { get; set; }

        [JsonProperty("connections")]
        public Dictionary<string, ItemConnectionData> DeviceConnections { get; set; }

        public Connections()
        {
            this.DeviceConnections = new Dictionary<string, ItemConnectionData>();
        }
    }
}
