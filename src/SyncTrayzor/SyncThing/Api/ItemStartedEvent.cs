using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.Api
{
    public class ItemStartedEventDetails
    {
        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Flags")]
        public long Flags { get; set; }

        [JsonProperty("Modified")]
        public long Modified { get; set; } // Is this supposed to be a DateTime?

        [JsonProperty("Version")]
        public long Version { get; set; }

        [JsonProperty("LocalVersion")]
        public long LocalVersion { get; set; }

        [JsonProperty("NumBlocks")]
        public long NumBlocks { get; set; }
    }

    public class ItemStartedEventData
    {
        [JsonProperty("item")]
        public string Item { get; set; }

        [JsonProperty("folder")]
        public string Folder { get; set; }

        [JsonProperty("details")]
        public ItemStartedEventDetails Details { get; set; }
    }

    public class ItemStartedEvent : Event
    {
        [JsonProperty("data")]
        public ItemStartedEventData Data { get; set; }

        public override void Visit(IEventVisitor visitor)
        {
            visitor.Accept(this);
        }

        public override string ToString()
        {
            return String.Format("<ItemStarted ID={0} Time={1} Item={2} Folder={3} Name={4} Flags={5} Modified={6} Version={7} LocalVersion={8} NumBlocks={9}>",
                this.Id, this.Time, this.Data.Item, this.Data.Folder, this.Data.Details.Name, this.Data.Details.Flags, this.Data.Details.Modified,
                this.Data.Details.Version, this.Data.Details.LocalVersion, this.Data.Details.NumBlocks);
        }
    }
}
