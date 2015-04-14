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
    public class AuthenticatedHttpClientHandler : WebRequestHandler
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly string apiKey;

        public AuthenticatedHttpClientHandler(string apiKey)
        {
            this.apiKey = apiKey;
            // We expect Syncthing to return invalid certs
            this.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add("X-API-Key", this.apiKey);
            if (logger.IsDebugEnabled)
            {
                var response = await base.SendAsync(request, cancellationToken);
                logger.Debug((await response.Content.ReadAsStringAsync()).Trim());
                return response;
            }
            else
            {
                return await base.SendAsync(request, cancellationToken);
            }
        }
    }
}
