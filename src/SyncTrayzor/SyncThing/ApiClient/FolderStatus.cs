using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            return String.Format("<FolderStatus GlobalBytes={0} GlobalDeleted={1} GlobalFiles={2} LocalBytes={3} LocalDeleted={4} LocalFiles={5} " +
                "InSyncBytes={6} InSyncFiles={7} NeedBytes={8} NeedFiles={9} IgnorePattners={10} Invalid={11} State={12} StateChanged={13} Version={14}>",
                this.GlobalBytes, this.GlobalDeleted, this.GlobalFiles, this.LocalBytes, this.LocalDeleted, this.LocalFiles,
                this.InSyncBytes, this.InSyncFiles, this.NeedBytes, this.NeedFiles, this.IgnorePatterns, this.Invalid, this.State, this.StateChanged, this.Version);
        }
    }
}
