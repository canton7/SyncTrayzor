using Stylet;
using SyncTrayzor.Services.Config;

namespace SyncTrayzor.Pages.BarAlerts
{
    public class IntelXeGraphicsAlertViewModel : Screen, IBarAlert
    {
        private readonly IConfigurationProvider configurationProvider;

        public AlertSeverity Severity => AlertSeverity.Info;

        public IntelXeGraphicsAlertViewModel(IConfigurationProvider configurationProvider)
        {
            this.configurationProvider = configurationProvider;
        }

        public void Dismiss()
        {
            this.configurationProvider.AtomicLoadAndSave(config => config.HideIntelXeWarningMessage = true);
        }
    }
}
