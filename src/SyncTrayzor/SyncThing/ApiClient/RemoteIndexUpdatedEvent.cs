using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.ApiClient
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
            return $"<RemoteIndexUpdatedEvent ID={this.Id} Time={this.Time} Device={this.Data.Device} Folder={this.Data.Folder} Items={this.Data.Items}>";
        }
    }
}
