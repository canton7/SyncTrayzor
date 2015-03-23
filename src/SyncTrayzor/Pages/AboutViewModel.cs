using Stylet;
using SyncTrayzor.Localization;
using SyncTrayzor.Properties;
using SyncTrayzor.Services;
using SyncTrayzor.Services.Config;
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
        private readonly IWindowManager windowManager;
        private readonly ISyncThingManager syncThingManager;
        private readonly IUpdateChecker updateChecker;
        private readonly Func<ThirdPartyComponentsViewModel> thirdPartyComponentsViewModelFactory;

        public string Version { get; set; }
        public string SyncthingVersion { get; set; }
        public string HomepageUrl { get; set; }

        public string NewerVersion { get; set; }
        public bool ShowTranslatorAttributation
        {
            get { return Localizer.Translate("TranslatorAttributation") == Localizer.OriginalTranslation("TranslatorAttributation"); }
        }
        private string newerVersionDownloadUrl;

        public AboutViewModel(
            IWindowManager windowManager,
            ISyncThingManager syncThingManager,
            IConfigurationProvider configurationProvider,
            IUpdateChecker updateChecker,
            Func<ThirdPartyComponentsViewModel> thirdPartyComponentsViewModelFactory)
        {
            this.windowManager = windowManager;
            this.syncThingManager = syncThingManager;
            this.updateChecker = updateChecker;
            this.thirdPartyComponentsViewModelFactory = thirdPartyComponentsViewModelFactory;

            this.Version = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
            this.HomepageUrl = Settings.Default.HomepageUrl;

            this.SyncthingVersion = this.syncThingManager.Version == null ? Localizer.Translate("AboutView_UnknownVersion") : this.syncThingManager.Version.Version;
            this.syncThingManager.DataLoaded += (o, e) =>
            {
                this.SyncthingVersion = this.syncThingManager.Version == null ? Localizer.Translate("AboutView_UnknownVersion") : this.syncThingManager.Version.Version;
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

        public void ShowLicenses()
        {
            var vm = this.thirdPartyComponentsViewModelFactory();
            this.windowManager.ShowDialog(vm);
            this.RequestClose(true);
        }

        public void Close()
        {
            this.RequestClose(true);
        }
    }
}
