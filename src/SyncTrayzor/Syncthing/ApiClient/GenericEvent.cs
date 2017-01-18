using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SyncTrayzor.Syncthing.ApiClient
{
    public class GenericEvent : Event
    {
        public override bool IsValid => true;

        public override void Visit(IEventVisitor visitor)
        {
            visitor.Accept(this);
        }

        [JsonProperty("data")]
        public JToken Data { get; set; }

        public override string ToString()
        {
            return $"<GenericEvent ID={this.Id} Type={this.Type} Time={this.Time} Data={this.Data.ToString(Formatting.None)}>";
        }
    }
}
