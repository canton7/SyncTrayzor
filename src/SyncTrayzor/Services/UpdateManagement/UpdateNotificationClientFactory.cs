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
