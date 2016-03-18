using Newtonsoft.Json;
namespace SyncTrayzor.Syncthing.ApiClient
{
    public class DeviceRejectedEventData
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("device")]
        public string DeviceId { get; set; }
    }

    public class DeviceRejectedEvent : Event
    {
        [JsonProperty("data")]
        public DeviceRejectedEventData Data { get; set; }

        public override void Visit(IEventVisitor visitor)
        {
            visitor.Accept(this);
        }

        public override string ToString()
        {
            return $"<DeviceRejected ID={this.Id} Time={this.Time} Address={this.Data.Address} DeviceId={this.Data.DeviceId}>";
        }
    }
}
