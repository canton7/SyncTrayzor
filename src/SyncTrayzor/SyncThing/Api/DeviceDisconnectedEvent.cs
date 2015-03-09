using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.Api
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
            return String.Format("<Disconnected ID={0} Time={1} Error={2} Id={3}>", this.Id, this.Time, this.Data.Error, this.Data.Id);
        }
    }
}
