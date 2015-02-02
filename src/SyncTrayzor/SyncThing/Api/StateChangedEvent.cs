using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.Api
{
    public class StateChangedEventData
    {
        [JsonProperty("folder")]
        public string Folder { get; set; }

        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }

        [JsonProperty("duration")]
        public double DurationSeconds { get; set; }

        public TimeSpan Duration
        {
            get { return TimeSpan.FromSeconds(this.DurationSeconds); }
            set { this.DurationSeconds = value.TotalSeconds; }
        }
    }

    public class StateChangedEvent : Event
    {
        [JsonProperty("data")]
        public StateChangedEventData Data { get; set; }

        public override string ToString()
        {
            return String.Format("<StateChangedEvent ID={0} Time={1} Folder={2} From={3} To={4} Duration={5}>", this.Id, this.Time, this.Data.Folder, this.Data.From, this.Data.To, this.Data.Duration);
        }
    }
}
