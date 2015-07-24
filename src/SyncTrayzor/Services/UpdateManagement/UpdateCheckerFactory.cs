namespace SyncTrayzor.Services.UpdateManagement
{
    public interface IUpdateCheckerFactory
    {
        IUpdateChecker CreateUpdateChecker(string baseUrl, string variant);
    }

    public class UpdateCheckerFactory : IUpdateCheckerFactory
    {
        private readonly IAssemblyProvider assemblyProvider;
        private readonly IUpdateNotificationClientFactory updateNotificationClientFactory;

        public UpdateCheckerFactory(IAssemblyProvider assemblyProvider, IUpdateNotificationClientFactory updateNotificationClientFactory)
        {
            this.assemblyProvider = assemblyProvider;
            this.updateNotificationClientFactory = updateNotificationClientFactory;
        }

        public IUpdateChecker CreateUpdateChecker(string baseUrl, string variant)
        {
            return new UpdateChecker(this.assemblyProvider.Version, this.assemblyProvider.ProcessorArchitecture, variant, this.updateNotificationClientFactory.CreateUpdateNotificationClient(baseUrl));
        }
    }
}
