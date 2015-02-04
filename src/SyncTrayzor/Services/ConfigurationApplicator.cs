using SyncTrayzor.NotifyIcon;
using SyncTrayzor.SyncThing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Services
{
    public class ConfigurationApplicator
    {
        private readonly IConfigurationProvider configurationProvider;

        private readonly INotifyIconManager notifyIconManager;
        private readonly ISyncThingManager syncThingManager;
        private readonly AutostartProvider autostartProvider;

        public ConfigurationApplicator(
            IConfigurationProvider configurationProvider,
            INotifyIconManager notifyIconManager,
            ISyncThingManager syncThingManager,
            AutostartProvider autostartProvider)
        {
            this.configurationProvider = configurationProvider;
            this.configurationProvider.ConfigurationChanged += (o, e) => this.ApplyNewConfiguration(e.NewConfiguration);

            this.notifyIconManager = notifyIconManager;
            this.syncThingManager = syncThingManager;
            this.autostartProvider = autostartProvider;

            this.syncThingManager.StateChanged += (o, e) =>
            {
                if (e.NewState == SyncThingState.Running)
                    this.LoadFolders();
            };
        }

        public void ApplyConfiguration()
        {
            this.ApplyNewConfiguration(this.configurationProvider.Load());
        }

        private void ApplyNewConfiguration(Configuration configuration)
        {
            this.notifyIconManager.CloseToTray = configuration.CloseToTray;
            this.notifyIconManager.ShowOnlyOnClose = configuration.ShowTrayIconOnlyOnClose;

            this.syncThingManager.Address = configuration.SyncThingAddress;

            this.autostartProvider.SetAutoStart(configuration.StartOnLogon, configuration.StartMinimized);
        }

        private void LoadFolders()
        {
            var configuration = this.configurationProvider.Load();

            foreach (var newKey in this.syncThingManager.Folders.Keys.Except(configuration.Folders.Select(x => x.ID)))
            {
                configuration.Folders.Add(new FolderConfiguration(newKey, true));
            }

            configuration.Folders = configuration.Folders.Where(x => this.syncThingManager.Folders.Keys.Contains(x.ID)).ToList();

            this.configurationProvider.Save(configuration);
        }
    }
}
