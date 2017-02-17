using Newtonsoft.Json;
using System;

namespace SyncTrayzor.Syncthing.ApiClient
{
    public abstract class Event
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("type")]
        public EventType Type { get; set; }

        [JsonProperty("time")]
        public DateTime Time { get; set; }

        public abstract bool IsValid { get; }

        public abstract void Visit(IEventVisitor visitor);
    }
}
