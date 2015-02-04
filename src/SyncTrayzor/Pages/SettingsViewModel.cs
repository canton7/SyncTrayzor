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

        public bool ShowTrayIconOnlyOnClose { get; set;}
        public bool CloseToTray { get; set; }
        public bool StartSyncThingAutomatically { get; set; }
        public string SyncThingAddress { get; set; }
        public bool StartOnLogon { get; set; }
        public bool StartMinimized { get; set; }
        public BindableCollection<WatchedFolder> WatchedFolders { get; set; }

        public SettingsViewModel(IConfigurationProvider configurationProvider)
        {
            this.DisplayName = "Settings";

            this.configurationProvider = configurationProvider;

            var configuration = this.configurationProvider.Load();

            this.ShowTrayIconOnlyOnClose = configuration.ShowTrayIconOnlyOnClose;
            this.CloseToTray = configuration.CloseToTray;
            this.StartSyncThingAutomatically = configuration.StartSyncThingAutomatically;
            this.SyncThingAddress = configuration.SyncThingAddress;
            this.StartOnLogon = configuration.StartOnLogon;
            this.StartMinimized = configuration.StartMinimized;
            this.WatchedFolders = new BindableCollection<WatchedFolder>(configuration.Folders.Select(x => new WatchedFolder()
            {
                Folder = x.ID,
                IsSelected = x.IsWatched
            }));
        }

        public void Save()
        {
            var configuration = this.configurationProvider.Load();

            configuration.ShowTrayIconOnlyOnClose = this.ShowTrayIconOnlyOnClose;
            configuration.CloseToTray = this.CloseToTray;
            configuration.StartSyncThingAutomatically = this.StartSyncThingAutomatically;
            configuration.SyncThingAddress = this.SyncThingAddress;
            configuration.StartOnLogon = this.StartOnLogon;
            configuration.StartMinimized = this.StartMinimized;
            configuration.Folders = this.WatchedFolders.Select(x => new FolderConfiguration(x.Folder, x.IsSelected)).ToList();

            this.configurationProvider.Save(configuration);
            this.RequestClose(true);
        }

        public void Cancel()
        {
            this.RequestClose(false);
        }
    }
}
