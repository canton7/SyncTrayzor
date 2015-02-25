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
        public bool ShowSynchronizedBalloon { get; set; }
        public bool StartSyncThingAutomatically { get; set; }
        public string SyncThingAddress { get; set; }
        public string SyncThingApiKey { get; set; }

        public bool CanReadAutostart { get; set; }
        public bool CanWriteAutostart { get; set; }
        public bool CanReadOrWriteAutostart
        {
            get { return this.CanReadAutostart || this.CanWriteAutostart; }
        }
        public bool CanReadAndWriteAutostart
        {
            get { return this.CanReadAutostart && this.CanWriteAutostart; }
        }
        public bool StartOnLogon { get; set; }
        public bool StartMinimized { get; set; }
        public bool StartMinimizedEnabled
        {
            get { return this.CanReadAndWriteAutostart && this.StartOnLogon; }
        }

        public BindableCollection<WatchedFolder> WatchedFolders { get; set; }

        public SettingsViewModel(IConfigurationProvider configurationProvider, IAutostartProvider autostartProvider)
        {
            this.DisplayName = "Settings";

            this.configurationProvider = configurationProvider;

            var configuration = this.configurationProvider.Load();

            this.ShowTrayIconOnlyOnClose = configuration.ShowTrayIconOnlyOnClose;
            this.CloseToTray = configuration.CloseToTray;
            this.ShowSynchronizedBalloon = configuration.ShowSynchronizedBalloon;
            this.StartSyncThingAutomatically = configuration.StartSyncthingAutomatically;
            this.SyncThingAddress = configuration.SyncthingAddress;
            this.SyncThingApiKey = configuration.SyncthingApiKey;
            this.StartOnLogon = configuration.StartOnLogon;
            this.StartMinimized = configuration.StartMinimized;
            this.WatchedFolders = new BindableCollection<WatchedFolder>(configuration.Folders.Select(x => new WatchedFolder()
            {
                Folder = x.ID,
                IsSelected = x.IsWatched
            }));

            this.CanReadAutostart = autostartProvider.CanRead;
            this.CanWriteAutostart = autostartProvider.CanWrite;
        }

        public void Save()
        {
            var configuration = this.configurationProvider.Load();

            configuration.ShowTrayIconOnlyOnClose = this.ShowTrayIconOnlyOnClose;
            configuration.CloseToTray = this.CloseToTray;
            configuration.ShowSynchronizedBalloon = this.ShowSynchronizedBalloon;
            configuration.StartSyncthingAutomatically = this.StartSyncThingAutomatically;
            configuration.SyncthingAddress = this.SyncThingAddress;
            configuration.SyncthingApiKey = this.SyncThingApiKey;
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
