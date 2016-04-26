using NLog;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace SyncTrayzor.Syncthing.ApiClient
{
    public class SyncthingHttpClientHandler : WebRequestHandler
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public SyncthingHttpClientHandler()
        {
            // We expect Syncthing to return invalid certs
            this.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                logger.Trace(() => response.Content.ReadAsStringAsync().Result.Trim());
            else
                logger.Warn("Non-successful status code. {0} {1}", response, (await response.Content.ReadAsStringAsync()).Trim());

            return response;
        }
    }
}
