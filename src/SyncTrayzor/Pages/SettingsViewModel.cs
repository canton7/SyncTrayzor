using Stylet;
using SyncTrayzor.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Pages
{
    public class SettingsViewModel : Screen
    {
        private readonly IConfigurationProvider configurationProvider;
        private Configuration configuration;

        public bool ShowTrayIconOnlyOnClose
        {
            get { return this.configuration.ShowTrayIconOnlyOnClose; }
            set { this.configuration.ShowTrayIconOnlyOnClose = value; }
        }

        public bool CloseToTray
        {
            get { return this.configuration.CloseToTray; }
            set { this.configuration.CloseToTray = value; }
        }

        public bool StartSyncThingAutomatically
        {
            get { return this.configuration.StartSyncThingAutomatically; }
            set { this.configuration.StartSyncThingAutomatically = value; }
        }

        public string SyncThingAddress
        {
            get { return this.configuration.SyncThingAddress; }
            set { this.configuration.SyncThingAddress = value; }
        }

        public bool StartOnLogon
        {
            get { return this.configuration.StartOnLogon; }
            set
            {
                this.configuration.StartOnLogon = value;
                this.NotifyOfPropertyChange(); // Needed to StartMinimized enabledness
            }
        }

        public bool StartMinimized
        {
            get { return this.configuration.StartMinimized; }
            set { this.configuration.StartMinimized = value; }
        }

        public SettingsViewModel(IConfigurationProvider configurationProvider)
        {
            this.DisplayName = "Settings";

            this.configurationProvider = configurationProvider;
            this.configuration = this.configurationProvider.Load();
        }

        public void Save()
        {
            this.configurationProvider.Save(this.configuration);
            this.RequestClose(true);
        }

        public void Cancel()
        {
            this.RequestClose(false);
        }
    }
}
