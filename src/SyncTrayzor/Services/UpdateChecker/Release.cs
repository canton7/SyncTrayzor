using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Services.UpdateChecker
{
    public class Release
    {
        public Version Version { get; private set; }
        public string DownloadUrl { get; private set; }

        public Release(Version version, string downloadUrl)
        {
            this.Version = version;
            this.DownloadUrl = downloadUrl;
        }
    }
}
