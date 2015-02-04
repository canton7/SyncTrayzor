using Stylet;
using SyncTrayzor.Services;
using SyncTrayzor.SyncThing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Pages
{
    public class WatchedFolder
    {
        public string Folder { get; set; }
        public bool IsSelected { get; set; }
    }

    public class SettingsViewModel : Screen
    {
        private readonly IConfigurationProvider configurationProvider;
        private readonly ISyncThingManager syncThingManager;

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

        public BindableCollection<WatchedFolder> WatchedFolders { get; set; }

        public SettingsViewModel(IConfigurationProvider configurationProvider, ISyncThingManager syncThingManager)
        {
            this.DisplayName = "Settings";

            this.configurationProvider = configurationProvider;
            this.syncThingManager = syncThingManager;

            this.configuration = this.configurationProvider.Load();
            if (this.syncThingManager.Folders != null)
            {
                this.WatchedFolders = new BindableCollection<WatchedFolder>(this.syncThingManager.Folders.Select(x => new WatchedFolder()
                { 
                    Folder = x.Key,
                    IsSelected = this.configuration.WatchedFolders.Contains(x.Key)
                }));
            }
        }

        public void Save()
        {
            // Only change if SyncThing has loaded and we were able to load its list of folders
            if (this.WatchedFolders != null)
                this.configuration.WatchedFolders = this.WatchedFolders.Where(x => x.IsSelected).Select(x => x.Folder).ToList();

            this.configurationProvider.Save(this.configuration);
            this.RequestClose(true);
        }

        public void Cancel()
        {
            this.RequestClose(false);
        }
    }
}
