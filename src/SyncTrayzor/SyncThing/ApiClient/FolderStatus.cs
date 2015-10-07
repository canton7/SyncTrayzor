using Newtonsoft.Json;
using System;

namespace SyncTrayzor.SyncThing.ApiClient
{
    public class FolderStatus
    {
        [JsonProperty("globalBytes")]
        public long GlobalBytes { get; set; }

        [JsonProperty("globalDeleted")]
        public int GlobalDeleted { get; set; }

        [JsonProperty("globalFiles")]
        public int GlobalFiles { get; set; }

        [JsonProperty("localBytes")]
        public long LocalBytes { get; set; }

        [JsonProperty("localDeleted")]
        public int LocalDeleted { get; set; }

        [JsonProperty("localFiles")]
        public int LocalFiles { get; set; }

        [JsonProperty("inSyncBytes")]
        public long InSyncBytes { get; set; }

        [JsonProperty("inSyncFiles")]
        public int InSyncFiles { get; set; }

        [JsonProperty("needBytes")]
        public long NeedBytes { get; set; }

        [JsonProperty("needFiles")]
        public int NeedFiles { get; set; }

        [JsonProperty("ignorePatterns")]
        public bool IgnorePatterns { get; set; }

        [JsonProperty("invalid")]
        public string Invalid { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("stateChanged")]
        public DateTime StateChanged { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; }

        public override string ToString()
        {
            return $"<FolderStatus GlobalBytes={this.GlobalBytes} GlobalDeleted={this.GlobalDeleted} GlobalFiles={this.GlobalFiles} " +
                $"LocalBytes={this.LocalBytes} LocalDeleted={this.LocalDeleted} LocalFiles={this.LocalFiles} " +
                $"InSyncBytes={this.InSyncBytes} InSyncFiles={this.InSyncFiles} NeedBytes={this.NeedBytes} NeedFiles={this.NeedFiles} " +
                $"IgnorePattners={this.IgnorePatterns}, Invalid={this.Invalid} State={this.State} StateChanged={this.StateChanged} Version={this.Version}>";
        }
    }
}
