using Stylet;
using SyncTrayzor.Localization;
using SyncTrayzor.Properties;
using SyncTrayzor.Services;
using SyncTrayzor.Services.Config;
using SyncTrayzor.Services.UpdateManagement;
using SyncTrayzor.Syncthing;
using System;
using System.Reflection;

namespace SyncTrayzor.Pages
{
    public class AboutViewModel : Screen
    {
        // Not in the app.config, in case some sysadmin wants to change it
        private readonly IWindowManager windowManager;
        private readonly ISyncthingManager syncthingManager;
        private readonly IUpdateManager updateManager;
        private readonly Func<ThirdPartyComponentsViewModel> thirdPartyComponentsViewModelFactory;
        private readonly IProcessStartProvider processStartProvider;

        public string Version { get; set; }
        public string SyncthingVersion { get; set; }
        public string HomepageUrl { get; set; }

        public string NewerVersion { get; set; }
        public bool ShowTranslatorAttributation => Localizer.Translate("TranslatorAttributation") == Localizer.OriginalTranslation("TranslatorAttributation");
        private string newerVersionDownloadUrl;

        public IDonationManager DonationManager { get; }

        public AboutViewModel(
            IWindowManager windowManager,
            ISyncthingManager syncthingManager,
            IUpdateManager updateManager,
            Func<ThirdPartyComponentsViewModel> thirdPartyComponentsViewModelFactory,
            IProcessStartProvider processStartProvider,
            IDonationManager donationManager)
        {
            this.windowManager = windowManager;
            this.syncthingManager = syncthingManager;
            this.updateManager = updateManager;
            this.thirdPartyComponentsViewModelFactory = thirdPartyComponentsViewModelFactory;
            this.processStartProvider = processStartProvider;
            this.DonationManager = donationManager;

            this.Version = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
            this.HomepageUrl = AppSettings.Instance.HomepageUrl;

            this.syncthingManager.DataLoaded += this.SyncthingDataLoaded;
            this.LoadSyncthingVersion();

            this.CheckForNewerVersionAsync();
        }

        private void SyncthingDataLoaded(object sender, EventArgs e)
        {
            this.LoadSyncthingVersion();
        }

        private void LoadSyncthingVersion()
        {
            this.SyncthingVersion = this.syncthingManager.Version == null ? Resources.AboutView_UnknownVersion : this.syncthingManager.Version.ShortVersion;
        }

        private async void CheckForNewerVersionAsync()
        {
            var results = await this.updateManager.CheckForAcceptableUpdateAsync();

            if (results == null)
                return;

            this.NewerVersion = results.NewVersion.ToString(3);
            this.newerVersionDownloadUrl = results.ReleasePageUrl;
        }

        public void ShowHomepage()
        {
            this.processStartProvider.StartDetached(this.HomepageUrl);
        }

        public void DownloadNewerVersion()
        {
            if (this.newerVersionDownloadUrl == null)
                return;

            this.processStartProvider.StartDetached(this.newerVersionDownloadUrl);
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

        protected override void OnClose()
        {
            this.syncthingManager.DataLoaded -= this.SyncthingDataLoaded;
        }
    }
}
