using NLog;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Services.UpdateManagement
{
    public interface IUpdateNotificationClient
    {
        Task<UpdateNotificationResponse> FetchUpdateAsync(string version, string arch, string variant);
    }

    public class UpdateNotificationClient : IUpdateNotificationClient
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IUpdateNotificationApi api;

        public UpdateNotificationClient(string url)
        {
            this.api = RestService.For<IUpdateNotificationApi>(url);
        }

        public async Task<UpdateNotificationResponse> FetchUpdateAsync(string version, string arch, string variant)
        {
            var updates = await this.api.FetchUpdateAsync(version, arch, variant);
            logger.Debug("Fetched updates response: {0}", updates);
            return updates;
        }
    }
}
