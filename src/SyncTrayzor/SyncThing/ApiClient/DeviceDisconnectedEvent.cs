using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.ApiClient
{
    public class DeviceDisconnectedEventData
    {
        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class DeviceDisconnectedEvent : Event
    {
        [JsonProperty("data")]
        public DeviceDisconnectedEventData Data { get; set; }

        public override void Visit(IEventVisitor visitor)
        {
            visitor.Accept(this);
        }

        public override string ToString()
        {
            return $"<Disconnected ID={this.Id} Time={this.Time} Error={this.Data.Error} Id={this.Data.Id}>";
        }
    }
}
