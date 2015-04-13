using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Services.UpdateManagement
{
    public interface IUpdateNotificationClientFactory
    {
        IUpdateNotificationClient CreateUpdateNotificationClient(string url);
    }
    
    public class UpdateNotificationClientFactory : IUpdateNotificationClientFactory
    {
        public IUpdateNotificationClient CreateUpdateNotificationClient(string url)
        {
            return new UpdateNotificationClient(url);
        }
    }
}
