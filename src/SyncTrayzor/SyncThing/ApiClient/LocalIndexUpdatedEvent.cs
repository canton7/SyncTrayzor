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
        [JsonProperty("folder")]
        public string Folder { get; set; }

        [JsonProperty("items")]
        public int Items { get; set; }

        [JsonProperty("version")]
        public long Version { get; set; }
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
            return String.Format("<LocalIndexUpdated ID={0} Time={1} Folder={2} Items={3} Version={4}>", this.Id, this.Time, this.Data.Folder, this.Data.Items, this.Data.Version);
        }
    }
}
