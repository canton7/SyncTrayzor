using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Services.UpdateManagement
{
    public class UpdateNotificationData
    {
        [JsonProperty("version")]
        public string VersionRaw { get; set; }

        public Version Version
        {
            get { return String.IsNullOrWhiteSpace(this.VersionRaw) ? null : new Version(this.VersionRaw); }
            set { this.VersionRaw = value.ToString(3); }
        }

        [JsonProperty("direct_download_url")]
        public string DirectDownloadUrl { get; set; }

        [JsonProperty("sha1sum_download_url")]
        public string Sha1sumDownloadUrl { get; set; }

        [JsonProperty("release_page_url")]
        public string ReleasePageUrl { get; set; }

        [JsonProperty("release_notes")]
        public string ReleaseNotes { get; set; }

        public override string ToString()
        {
            return String.Format("<UpdateNotificationData version={0} direct_download_url={1} sha1sum_download_url={2} release_page_url={3} release_notes={4}>",
                this.Version.ToString(3), this.DirectDownloadUrl, this.Sha1sumDownloadUrl, this.ReleasePageUrl, this.ReleaseNotes);
        }
    }

    public class UpdateNotificationError
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        public override string ToString()
        {
            return String.Format("<UpdateNotificationError code={0} message={1}>", this.Code, this.Message);
        }
    }

    public class UpdateNotificationResponse
    {
        [JsonProperty("data")]
        public UpdateNotificationData Data { get; set; }

        [JsonProperty("error")]
        public UpdateNotificationError Error { get; set; }

        public override string ToString()
        {
            return String.Format("<UpdateNotificationResponse data={0} error={1}>", this.Data, this.Error);
        }
    }
}
