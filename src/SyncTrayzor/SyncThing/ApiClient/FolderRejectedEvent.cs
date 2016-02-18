using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Syncthing.ApiClient
{
    public class FolderRejectedEventData
    {
        [JsonProperty("device")]
        public string DeviceId { get; set; }

        [JsonProperty("folder")]
        public string FolderId { get; set; }
    }

    public class FolderRejectedEvent : Event
    {
        [JsonProperty("data")]
        public FolderRejectedEventData Data { get; set; }

        public override void Visit(IEventVisitor visitor)
        {
            visitor.Accept(this);
        }

        public override string ToString()
        {
            return $"<FolderRejected ID={this.Id} Time={this.Time} DeviceId={this.Data.DeviceId} FolderId={this.Data.FolderId}>";
        }
    }
}
