using Newtonsoft.Json;
using System;

namespace SyncTrayzor.Syncthing.ApiClient
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
            get => TimeSpan.FromSeconds(this.DurationSeconds);
            set => this.DurationSeconds = value.TotalSeconds;
        }
    }

    public class StateChangedEvent : Event
    {
        [JsonProperty("data")]
        public StateChangedEventData Data { get; set; }

        public override bool IsValid => this.Data != null &&
            !string.IsNullOrWhiteSpace(this.Data.Folder) &&
            !string.IsNullOrWhiteSpace(this.Data.From) &&
            !string.IsNullOrWhiteSpace(this.Data.To) &&
            this.Data.Duration >= TimeSpan.Zero;

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
