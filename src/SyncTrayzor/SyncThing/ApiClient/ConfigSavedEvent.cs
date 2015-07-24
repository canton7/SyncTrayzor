using Newtonsoft.Json;

namespace SyncTrayzor.SyncThing.ApiClient
{
    public class ConfigSavedEvent : Event
    {
        [JsonProperty("data")]
        public Config Data { get; set; }

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
