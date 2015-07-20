using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.ApiClient
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

        public override void Visit(IEventVisitor visitor)
        {
            visitor.Accept(this);
        }

        public override string ToString()
        {
            return $"<StateChangedEvent ID={this.Id} Time={this.Time} Folder={this.Data.Folder} From={this.Data.From} To={this.Data.To} Duration={this.Data.Duration}>";
        }
    }
}
