using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.ApiClient
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
            return String.Format("<Version arch={0} longVersion={1} os={2} version={3}>", this.Arch, this.LongVersion, this.OS, this.Version);
        }
    }
}
