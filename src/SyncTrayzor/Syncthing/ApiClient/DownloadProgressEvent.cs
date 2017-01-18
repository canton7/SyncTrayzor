using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;

namespace SyncTrayzor.Syncthing.ApiClient
{
    public class DownloadProgressEventFileData
    {
        /// <summary>
        /// Total number of blocks in this file
        /// </summary>
        [JsonProperty("Total")]
        public long Total { get; set; }

        /// <summary>
        /// Number of blocks currently being downloaded
        /// </summary>
        [JsonProperty("Pulling")]
        public int Pulling { get; set; }

        /// <summary>
        /// Number of blocks copied from the file we are about to replace
        /// </summary>
        [JsonProperty("CopiedFromOrigin")]
        public int CopiedFromOrigin { get; set; }

        /// <summary>
        /// Number of blocks reused from a previous temporary file
        /// </summary>
        [JsonProperty("Reused")]
        public int Reused { get; set; }

        /// <summary>
        /// Number of blocks copied from other files or potentially other folders
        /// </summary>
        [JsonProperty("CopedFromElsewhere")]
        public int CopiedFromElsewhere { get; set; }

        /// <summary>
        /// Number of blocks actually downloaded so far
        /// </summary>
        [JsonProperty("Pulled")]
        public int Pulled { get; set; }

        /// <summary>
        /// Approximate total file size
        /// </summary>
        [JsonProperty("BytesTotal")]
        public long BytesTotal { get; set; }

        /// <summary>
        /// Approximate number of bytes already handled (already reused, copied, or pulled)
        /// </summary>
        [JsonProperty("BytesDone")]
        public long BytesDone { get; set; }
    }

    public class DownloadProgressEvent : Event
    {
        [JsonProperty("data")]
        public Dictionary<string, Dictionary<string, DownloadProgressEventFileData>> Data { get; set; }

        public override bool IsValid => this.Data != null;

        public override void Visit(IEventVisitor visitor)
        {
            visitor.Accept(this);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var folder in this.Data)
            {
                foreach (var file in folder.Value)
                {
                    sb.AppendFormat("{0}:{1}={2}/{3}", folder.Key, file.Key, file.Value.BytesDone, file.Value.BytesTotal);
                }
            }

            return $"<DownloadProgress ID={this.Id} Time={this.Time} {sb.ToString()}>";
        }
    }
}
