using RestEase;
using System.Threading.Tasks;

namespace SyncTrayzor.Services.UpdateManagement
{
    public interface IUpdateNotificationApi
    {
        [Get("")]
        Task<UpdateNotificationResponse> FetchUpdateAsync(string version, string arch, string variant);
    }
}
