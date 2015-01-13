using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public interface ISyncThingApiClient
    {
        string ApiKey { get; set; }
        Uri BaseAddress { get; set; }

        Task ShutdownAsync();
    }

    public class SyncThingApiClient : ISyncThingApiClient
    {
        private readonly HttpClient httpClient;
        private readonly ISyncThingApi api;

        public string ApiKey { get; set; }
        public Uri BaseAddress
        {
            get { return this.httpClient.BaseAddress; }
            set { this.httpClient.BaseAddress = value; }
        }

        public SyncThingApiClient()
        {
            this.httpClient = new HttpClient(new AuthenticatedHttpClientHandler(() => this.ApiKey));
            this.api = RestService.For<ISyncThingApi>(this.httpClient);
        }

        public Task ShutdownAsync()
        {
            this.EnsureSetup();
            return this.api.ShutdownAsync();
        }

        private void EnsureSetup()
        {
            if (this.BaseAddress == null)
                throw new InvalidOperationException("BaseAddress not set");
        }

        private class AuthenticatedHttpClientHandler : HttpClientHandler
        {
            private readonly Func<string> apiKey;

            public AuthenticatedHttpClientHandler(Func<string> apiKey)
            {
                this.apiKey = apiKey;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            {
                request.Headers.Add("X-API-Key", this.apiKey());
                return base.SendAsync(request, cancellationToken);
            }
        }
    }
}
