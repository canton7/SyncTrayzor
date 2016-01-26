using Newtonsoft.Json;

namespace SyncTrayzor.Syncthing.ApiClient
{
    public class DeviceResumedEventData
    {
        [JsonProperty("device")]
        public string DeviceId { get; set; }
    }

    public class DeviceResumedEvent : Event
    {
        [JsonProperty("data")]
        public DevicePausedEventData Data { get; set; }

        public override void Visit(IEventVisitor visitor)
        {
            visitor.Accept(this);
        }

        public override string ToString()
        {
            return $"<DeviceResumed ID={this.Id} Time={this.Time} DeviceId={this.Data.DeviceId}>";
        }
    }
}
