using Newtonsoft.Json;

namespace SyncTrayzor.Syncthing.ApiClient
{
    public class DevicePausedEventData
    {
        [JsonProperty("device")]
        public string DeviceId { get; set; }
    }

    public class DevicePausedEvent : Event
    {
        [JsonProperty("data")]
        public DevicePausedEventData Data { get; set; }

        public override bool IsValid => this.Data != null &&
            !string.IsNullOrWhiteSpace(this.Data.DeviceId);

        public override void Visit(IEventVisitor visitor)
        {
            visitor.Accept(this);
        }

        public override string ToString()
        {
            return $"<DevicePaused ID={this.Id} Time={this.Time} DeviceId={this.Data.DeviceId}>";
        }
    }
}
