using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.ApiClient
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

        public override void Visit(IEventVisitor visitor)
        {
            visitor.Accept(this);
        }

        public override string ToString()
        {
            return String.Format("<ItemFinished ID={0} Time={1} Item={2} Folder={3} Error={4}>", this.Id, this.Time, this.Data.Item, this.Data.Folder, this.Data.Error);
        }
    }
}
