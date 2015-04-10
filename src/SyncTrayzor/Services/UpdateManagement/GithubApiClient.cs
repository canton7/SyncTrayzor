using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Services.UpdateManagement
{
    public interface IGithubApiClient
    {
        void SetConnectionDetails(Uri baseAddress);
        Task<Release> FetchLatestReleaseAsync();
    }

    public class GithubApiClient : IGithubApiClient
    {
        private IGithubApi api;

        public void SetConnectionDetails(Uri baseAddress)
        {
            var httpClient = new HttpClient()
            {
                BaseAddress = baseAddress,
            };

            this.api = RestService.For<IGithubApi>(httpClient);
        }

        public async Task<Release> FetchLatestReleaseAsync()
        {
            var releases = await this.api.FetchReleasesAsync();

            var latestRelease = (from release in releases
                                where !release.IsDraft && !release.IsPrerelease
                                where release.Assets.Any(assert => assert.ContentType == "application/octet-stream")
                                let version = new Version(release.TagName.TrimStart('v'))
                                orderby version descending
                                select new Release(version, release.Url, release.Body)).FirstOrDefault();

            return latestRelease;
        }
    }
}
