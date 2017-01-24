using Newtonsoft.Json;

namespace SyncTrayzor.Syncthing.ApiClient
{
    public class FolderRejectedEventData
    {
        [JsonProperty("device")]
        public string DeviceId { get; set; }

        [JsonProperty("folder")]
        public string FolderId { get; set; }
    }

    public class FolderRejectedEvent : Event
    {
        [JsonProperty("data")]
        public FolderRejectedEventData Data { get; set; }

        public override bool IsValid => this.Data != null &&
            !string.IsNullOrWhiteSpace(this.Data.DeviceId) &&
            !string.IsNullOrWhiteSpace(this.Data.FolderId);

        public override void Visit(IEventVisitor visitor)
        {
            visitor.Accept(this);
        }

        public override string ToString()
        {
            return $"<FolderRejected ID={this.Id} Time={this.Time} DeviceId={this.Data.DeviceId} FolderId={this.Data.FolderId}>";
        }
    }
}
