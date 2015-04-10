using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Services.UpdateManagement
{
    public class Release
    {
        public Version Version { get; private set; }
        public string DownloadUrl { get; private set; }
        public string Body { get; private set; }

        public Release(Version version, string downloadUrl, string body)
        {
            this.Version = version;
            this.DownloadUrl = downloadUrl;
            this.Body = body;
        }
    }
}
