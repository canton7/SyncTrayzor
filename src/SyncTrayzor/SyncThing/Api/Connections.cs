using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.Api
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
    }

    public class Connections
    {
        [JsonProperty("total")]
        public ItemConnectionData Total { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JToken> DeviceConnectionsRaw { get; set; }

        private Dictionary<string, ItemConnectionData> _deviceConnections;
        public Dictionary<string, ItemConnectionData> DeviceConnections
        {
            get
            {
                if (this._deviceConnections == null)
                {
                    this._deviceConnections = this.DeviceConnectionsRaw == null ?
                        new Dictionary<string, ItemConnectionData>() :
                        this.DeviceConnectionsRaw.ToDictionary(x => x.Key, x => x.Value.ToObject<ItemConnectionData>());
                }
                return this._deviceConnections;
            }
        }
    }
}
