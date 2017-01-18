using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SyncTrayzor.Syncthing.ApiClient
{
    public class ItemFinishedEventData
    {
        [JsonProperty("item")]
        public string Item { get; set; }

        [JsonProperty("folder")]
        public string Folder { get; set; }

        // Irritatingly, 'error' is currently a structure containing an 'Err' property,
        // but in the future may just become a string....

        [JsonProperty("error")]
        public JToken ErrorRaw { get; set; }

        public string Error
        {
            get
            {
                if (this.ErrorRaw == null)
                    return null;
                if (this.ErrorRaw.Type == JTokenType.String)
                    return (string)this.ErrorRaw;
                if (this.ErrorRaw.Type == JTokenType.Object)
                    return (string)((JObject)this.ErrorRaw)["Err"];
                return null;
            }
        }

        [JsonProperty("type")]
        public ItemChangedItemType Type { get; set; }

        [JsonProperty("action")]
        public ItemChangedActionType Action { get; set; }
    }

    public class ItemFinishedEvent : Event
    {
        [JsonProperty("data")]
        public ItemFinishedEventData Data { get; set; }

        public override bool IsValid => this.Data != null &&
            !string.IsNullOrWhiteSpace(this.Data.Item) &&
            !string.IsNullOrWhiteSpace(this.Data.Folder) &&
            this.Data.Type != ItemChangedItemType.Unknown &&
            this.Data.Action != ItemChangedActionType.Unknown;

        public override void Visit(IEventVisitor visitor)
        {
            visitor.Accept(this);
        }

        public override string ToString()
        {
            return $"<ItemFinished ID={this.Id} Time={this.Time} Item={this.Data.Item} Folder={this.Data.Folder} Error={this.Data.Error}>";
        }
    }
}
