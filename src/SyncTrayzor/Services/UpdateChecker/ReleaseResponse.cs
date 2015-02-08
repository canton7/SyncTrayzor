using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Services.UpdateChecker
{
    public class ReleaseAssetResponse
    {
        [JsonProperty("browser_download_url")]
        public string DownloadUrl { get; set; }

        [JsonProperty("content_type")]
        public string ContentType { get; set; }
    }

    public class ReleaseResponse
    {
        [JsonProperty("tag_name")]
        public string TagName { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("draft")]
        public bool IsDraft { get; set; }

        [JsonProperty("prerelease")]
        public bool IsPrerelease { get; set; }

        [JsonProperty("published_at")]
        public DateTime PublishedAt { get; set; }

        [JsonProperty("assets")]
        public List<ReleaseAssetResponse> Assets { get; set; }
    }
}
