using NLog;
using RestEase;
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
            this.api = RestClient.For<IUpdateNotificationApi>(url);
        }

        public async Task<UpdateNotificationResponse> FetchUpdateAsync(string version, string arch, string variant)
        {
            var updates = await this.api.FetchUpdateAsync(version, arch, variant);
            logger.Debug("Fetched updates response: {0}", updates);
            return updates;
        }
    }
}
