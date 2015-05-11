using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.ApiClient
{
    public class ItemFinishedEventDataError
    {
        [JsonProperty("Op")]
        public string Op { get; set; }

        [JsonProperty("Path")]
        public string Path { get; set; }

        [JsonProperty("Err")]
        public int ErrorCode { get; set; }

        public override string ToString()
        {
            return String.Format("<Error Op={0} Path={1} Err={2}>", this.Op, this.Path, this.ErrorCode);
        }
    }

    public class ItemFinishedEventData
    {
        [JsonProperty("item")]
        public string Item { get; set; }

        [JsonProperty("folder")]
        public string Folder { get; set; }

        [JsonProperty("error")]
        public ItemFinishedEventDataError Error { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; }
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
