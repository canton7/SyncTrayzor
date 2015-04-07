using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.ApiClient
{
    public class LocalIndexUpdatedEventData
    {
        [JsonProperty("flags")]
        public string Flags { get; set; }

        [JsonProperty("modified")]
        public DateTime Modified { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("folder")]
        public string Folder { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }
    }

    public class LocalIndexUpdatedEvent : Event
    {
        [JsonProperty("data")]
        public LocalIndexUpdatedEventData Data { get; set; }

        public override void Visit(IEventVisitor visitor)
        {
            visitor.Accept(this);
        }

        public override string ToString()
        {
            return String.Format("<LocalIndexUpdated ID={0} Time={1} Flags={2} Modified={3} Name={4} Folder={5} Size={6}>", this.Id, this.Time, this.Data.Flags, this.Data.Modified, this.Data.Name, this.Data.Folder, this.Data.Size);
        }
    }
}
