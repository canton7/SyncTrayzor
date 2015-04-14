using NLog;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.ApiClient
{
    public interface ISyncThingApiClientFactory
    {
        Task<ISyncThingApiClient> CreateCorrectApiClientAsync(Uri baseAddress, string apiKey, CancellationToken cancellationToken);
    }

    public class SyncThingApiClientFactory : ISyncThingApiClientFactory
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public async Task<ISyncThingApiClient> CreateCorrectApiClientAsync(Uri baseAddress, string apiKey, CancellationToken cancellationToken)
        {
            // This is a bit fugly - there's no way to determine which one we're talking to without trying a request and have it fail...
            ISyncThingApiClient client = new SyncThingApiClientV0p10(baseAddress, apiKey);

            // Time everything so we break out in about 60 seconds
            int retryCount = 0;
            while (true)
            {
                try
                {
                    logger.Debug("Attempting to request API using version 0.10.x API client");
                    await client.FetchVersionAsync();
                    break;
                }
                catch (HttpRequestException)
                {
                    logger.Debug("HttpRequestException {0} of 20", retryCount);
                    // Expected when Syncthing's still starting
                    if (retryCount >= 60)
                        throw;
                    retryCount++;
                }
                catch (ApiException e)
                {
                    if (e.StatusCode != HttpStatusCode.NotFound)
                        throw;

                    logger.Debug("404 with 0.10.x API client - defaulting to 0.11.x");
                    client = new SyncThingApiClientV0p11(baseAddress, apiKey);
                    break;
                }

                await Task.Delay(1000, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }

            return client;
        }
    }
}
