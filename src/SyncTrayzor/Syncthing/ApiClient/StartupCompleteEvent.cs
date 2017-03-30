using Newtonsoft.Json;

namespace SyncTrayzor.Syncthing.ApiClient
{
    public class StartupCompleteEvent : Event
    {
        [JsonProperty("myID")]
        public string MyID { get; set; }

        public override bool IsValid => !string.IsNullOrWhiteSpace(this.MyID);

        public override void Visit(IEventVisitor visitor)
        {
            visitor.Accept(this);
        }

        public override string ToString()
        {
            return $"<StartupComplete ID={this.Id} Time={this.Time}>";
        }
    }
}
