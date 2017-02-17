using Newtonsoft.Json;

namespace SyncTrayzor.Syncthing.ApiClient
{
    public class ConfigSavedEvent : Event
    {
        [JsonProperty("data")]
        public Config Data { get; set; }

        public override bool IsValid => this.Data != null;

        public override void Visit(IEventVisitor visitor)
        {
            visitor.Accept(this);
        }

        public override string ToString()
        {
            return $"<ConfigSaved Config={this.Data}>";
        }
    }
}
