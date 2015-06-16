using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.ApiClient
{
    public class StartupCompleteEvent : Event
    {
        [JsonProperty("myID")]
        public string MyID { get; set; }

        public override void Visit(IEventVisitor visitor)
        {
            visitor.Accept(this);
        }

        public override string ToString()
        {
            return String.Format("<StartupComplete ID={0} Time={1}>", this.Id, this.Time);
        }
    }
}
