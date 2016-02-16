using NLog;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

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

            // We're getting null bodies from somewhere: try and figure out where
            var responseString = await response.Content.ReadAsStringAsync();
            if (responseString == null)
                logger.Warn($"Null response received from {request.RequestUri}. {response}. Content (again): {response.Content.ReadAsStringAsync()}");

            if (response.IsSuccessStatusCode)
                logger.Trace(() => response.Content.ReadAsStringAsync().Result.Trim());
            else
                logger.Warn("Non-successful status code. {0} {1}", response, (await response.Content.ReadAsStringAsync()).Trim());

            return response;
        }
    }
}
