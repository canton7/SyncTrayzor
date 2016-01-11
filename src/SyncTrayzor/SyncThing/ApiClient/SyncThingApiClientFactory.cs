using NLog;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.ApiClient
{
    public interface ISyncThingApiClientFactory
    {
        Task<ISyncThingApiClient> CreateCorrectApiClientAsync(Uri baseAddress, string apiKey, TimeSpan timeout, CancellationToken cancellationToken);
    }

    public class SyncThingApiClientFactory : ISyncThingApiClientFactory
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public async Task<ISyncThingApiClient> CreateCorrectApiClientAsync(Uri baseAddress, string apiKey, TimeSpan timeout, CancellationToken cancellationToken)
        {
            // This is a bit fugly - there's no way to determine which one we're talking to without trying a request and have it fail...
            ISyncThingApiClient client = new SyncThingApiClient(baseAddress, apiKey);

            // We abort because of the CancellationToken or because we take too long, or succeed
            // We used to measure absolute time here. However, we can be put to sleep halfway through this operation,
            // and so fail the timeout condition without actually trying for the appropriate amount of time.
            // Therefore, do it for a num iterations...
            var numAttempts = timeout.TotalSeconds; // Delay for 1 second per iteration
            bool success = false;
            Exception lastException = null;
            for (int retryCount = 0; retryCount < numAttempts; retryCount++)
            {
                try
                {
                    logger.Debug("Attempting to request API");
                    await client.FetchVersionAsync();
                    success = true;
                    logger.Debug("Success!");
                    break;
                }
                catch (HttpRequestException e)
                {
                    logger.Debug("Failed to connect on attempt {0}", retryCount);
                    // Expected when Syncthing's still starting
                    lastException = e;
                }

                await Task.Delay(1000, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (!success)
                throw new SyncThingDidNotStartCorrectlyException($"Syncthing didn't connect after {timeout}", lastException);

            return client;
        }
    }
}
