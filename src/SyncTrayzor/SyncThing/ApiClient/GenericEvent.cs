using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SyncTrayzor.SyncThing.ApiClient
{
    public class GenericEvent : Event
    {
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
