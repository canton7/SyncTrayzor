using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Services.UpdateManagement
{
    public interface IUpdateNotificationApi
    {
        [Get("")]
        Task<UpdateNotificationResponse> FetchUpdateAsync(string version, string arch, string variant);
    }
}
