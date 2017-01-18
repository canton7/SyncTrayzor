using Newtonsoft.Json;

namespace SyncTrayzor.Syncthing.ApiClient
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

        public override bool IsValid => this.Data != null &&
            !string.IsNullOrWhiteSpace(this.Data.Device) &&
            !string.IsNullOrWhiteSpace(this.Data.Folder) &&
            this.Data.Items >= 0;

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
