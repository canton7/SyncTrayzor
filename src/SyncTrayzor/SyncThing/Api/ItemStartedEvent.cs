using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.Api
{
    public class ItemStartedEventData
    {
        [JsonProperty("item")]
        public string Item { get; set; }

        [JsonProperty("folder")]
        public string Folder { get; set; }
    }

    public class ItemStartedEvent : Event
    {
        [JsonProperty("data")]
        public ItemStartedEventData Data { get; set; }

        public override void Visit(IEventVisitor visitor)
        {
            visitor.Accept(this);
        }

        public override string ToString()
        {
            return String.Format("<ItemStarted ID={0} Time={1} Item={2} Folder={3}>", this.Id, this.Time, this.Data.Item, this.Data.Folder);
        }
    }
}
