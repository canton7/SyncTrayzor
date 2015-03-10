using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.Api
{
    public class DeviceConnectedEventData
    {
        [JsonProperty("addr")]
        public string Address { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class DeviceConnectedEvent : Event
    {
        [JsonProperty("data")]
        public DeviceConnectedEventData Data { get; set; }

        public override void Visit(IEventVisitor visitor)
        {
            visitor.Accept(this);
        }

        public override string ToString()
        {
            return String.Format("<DeviceConnected ID={0} Time={1} Addr={2} Id={3}>", this.Id, this.Time, this.Data.Address, this.Data.Id);
        }
    }
}
