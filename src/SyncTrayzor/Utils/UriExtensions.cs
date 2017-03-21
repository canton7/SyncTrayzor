using System;

namespace SyncTrayzor.Utils
{
    public static class UriExtensions
    {
        public static Uri NormalizeZeroHost(this Uri uri)
        {
            if (uri.Host == "0.0.0.0")
            {
                var builder = new UriBuilder(uri)
                {
                    Host = "127.0.0.1"
                };
                uri = builder.Uri;
            }
            return uri;
        }
    }
}
