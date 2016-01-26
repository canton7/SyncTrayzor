using Newtonsoft.Json;

namespace SyncTrayzor.Syncthing.ApiClient
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
            return $"<DeviceConnected ID={this.Id} Time={this.Time} Addr={this.Data.Address} Id={this.Data.Id}>";
        }
    }
}
