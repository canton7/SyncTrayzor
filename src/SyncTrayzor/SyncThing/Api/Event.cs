using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.Api
{
    public abstract class Event
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("type")]
        public EventType Type { get; set; }

        [JsonProperty("time")]
        public DateTime Time { get; set; }
    }
}
