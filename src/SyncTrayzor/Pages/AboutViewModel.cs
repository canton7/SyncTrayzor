using Stylet;
using SyncTrayzor.Properties;
using SyncTrayzor.Services.UpdateChecker;
using SyncTrayzor.SyncThing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Pages
{
    public class AboutViewModel : Screen
    {
        private readonly ISyncThingManager syncThingManager;
        private readonly IUpdateChecker updateChecker;

        public string Version { get; set; }
        public string SyncthingVersion { get; set; }
        public string HomepageUrl { get; set; }

        public string NewerVersion { get; set; }
        private string newerVersionDownloadUrl;

        public AboutViewModel(ISyncThingManager syncThingManager, IUpdateChecker updateChecker)
        {
            this.syncThingManager = syncThingManager;
            this.updateChecker = updateChecker;

            this.Version = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
            this.HomepageUrl = Settings.Default.HomepageUrl;

            this.SyncthingVersion = this.syncThingManager.Version == null ? "Unknown" : this.syncThingManager.Version.Version;
            this.syncThingManager.DataLoaded += (o, e) =>
            {
                this.SyncthingVersion = this.syncThingManager.Version == null ? "Unknown" : this.syncThingManager.Version.Version;
            };

            this.CheckForNewerVersionAsync();
        }

        private async void CheckForNewerVersionAsync()
        {
            var results = await this.updateChecker.FetchUpdatesAsync();

            if (results == null)
                return;

            if (results.LatestVersionIsNewer)
            {
                this.NewerVersion = results.LatestVersion.ToString(3);
                this.newerVersionDownloadUrl = results.LatestVersionDownloadUrl;
            }
            else
            {
                this.NewerVersion = null;
                this.newerVersionDownloadUrl = null;
            }
        }

        public void ShowHomepage()
        {
            Process.Start(this.HomepageUrl);
        }

        public void DownloadNewerVersion()
        {
            if (this.newerVersionDownloadUrl == null)
                return;

            Process.Start(this.newerVersionDownloadUrl);
        }

        public void Close()
        {
            this.RequestClose(true);
        }
    }
}
