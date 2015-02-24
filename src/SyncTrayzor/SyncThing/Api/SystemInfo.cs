using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.Api
{
    public class SystemInfo
    {
        [JsonProperty("alloc")]
        public long AllocatedMemoryInUse { get; set; }

        [JsonProperty("cpuPercent")]
        public float CpuPercent { get; set; }

        // Ignore extAnnounceOK for now

        [JsonProperty("goroutines")]
        public long GoRoutines { get; set; }

        [JsonProperty("myID")]
        public string MyID { get; set; }

        [JsonProperty("sys")]
        public long AllocatedMemoryTotal { get; set; }

        [JsonProperty("tilde")]
        public string Tilde { get; set; }
    }
}
