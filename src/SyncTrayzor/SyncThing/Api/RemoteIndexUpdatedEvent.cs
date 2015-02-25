using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.Api
{
    public class RemoteIndexUpdatedEventData
    {
        [JsonProperty("device")]
        public string Device { get; set; }

        [JsonProperty("folder")]
        public string Folder { get; set; }

        [JsonProperty("items")]
        public long Items { get; set; }
    }

    public class RemoteIndexUpdatedEvent : Event
    {
        [JsonProperty("data")]
        public RemoteIndexUpdatedEventData Data { get; set; }

        public override void Visit(IEventVisitor visitor)
        {
            visitor.Accept(this);
        }

        public override string ToString()
        {
            return String.Format("<RemoteIndexUpdatedEvent ID={0} Time={1} Device={2} Folder={3} Items={4}>", this.Id, this.Time, this.Data.Device, this.Data.Folder, this.Data.Items);
        }
    }
}
