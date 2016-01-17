using Newtonsoft.Json;

namespace SyncTrayzor.Syncthing.ApiClient
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
            return $"<SystemInfo alloc={this.AllocatedMemoryInUse} cpuPercent={this.CpuPercent} goroutines={this.GoRoutines} myId={this.MyID} " +
                $"pathSeparator={this.PathSeparator} sys={this.AllocatedMemoryTotal} tilde={this.Tilde}>";
        }
    }
}
