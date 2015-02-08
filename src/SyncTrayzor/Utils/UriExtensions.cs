using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Utils
{
    public static class UriExtensions
    {
        public static Uri NormalizeZeroHost(this Uri uri)
        {
            if (uri.Host == "0.0.0.0")
            {
                var builder = new UriBuilder(uri);
                builder.Host = "127.0.0.1";
                uri = builder.Uri;
            }
            return uri;
        }
    }
}
