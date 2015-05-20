using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public class SyncThingHttpClientHandler : WebRequestHandler
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public SyncThingHttpClientHandler()
        {
            // We expect Syncthing to return invalid certs
            this.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (logger.IsTraceEnabled)
            {
                var response = await base.SendAsync(request, cancellationToken);
                logger.Trace((await response.Content.ReadAsStringAsync()).Trim());
                return response;
            }
            else
            {
                return await base.SendAsync(request, cancellationToken);
            }
        }
    }
}
