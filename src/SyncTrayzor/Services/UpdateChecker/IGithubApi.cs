using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Services.UpdateChecker
{
    [Headers("User-Agent: SyncTrayzor")]
    public interface IGithubApi
    {
        [Get("/releases")]
        Task<List<ReleaseResponse>> FetchReleasesAsync();
    }
}
