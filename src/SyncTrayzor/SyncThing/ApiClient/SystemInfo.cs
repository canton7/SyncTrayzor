using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.ApiClient
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

        [JsonProperty("pathSeparator")]
        public string PathSeparator { get; set; }

        [JsonProperty("sys")]
        public long AllocatedMemoryTotal { get; set; }

        [JsonProperty("tilde")]
        public string Tilde { get; set; }

        public override string ToString()
        {
            return String.Format("<SystemInfo alloc={0} cpuPercent={1} goroutines={2} myId={3} pathSeparator={4} sys={5} tilde={6}>",
                this.AllocatedMemoryInUse, this.CpuPercent, this.GoRoutines, this.MyID, this.PathSeparator, this.AllocatedMemoryTotal, this.Tilde);
        }
    }
}
