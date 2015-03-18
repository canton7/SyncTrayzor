using FluentValidation;
using Stylet;
using SyncTrayzor.Localization;
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

    public class SettingsViewModelValidator : AbstractValidator<SettingsViewModel>
    {
        private const string apiKeyChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-";

        public SettingsViewModelValidator()
        {
            RuleFor(x => x.SyncThingAddress).NotEmpty().WithMessage(Localizer.Translate("SettingsView_Validation_NotShouldBeEmpty"));
            RuleFor(x => x.SyncThingAddress).Must(str =>
            {
                Uri uri;
                return Uri.TryCreate(str, UriKind.Absolute, out uri) && uri.IsWellFormedOriginalString() &&
                    (uri.Scheme == "http" || uri.Scheme == "https");
            }).WithMessage(Localizer.Translate("String1SettingsView_Validation_InvalidUrl"));

            RuleFor(x => x.SyncThingApiKey).NotEmpty().WithMessage(Localizer.Translate("SettingsView_Validation_NotShouldBeEmpty"));
        }
    }

    public class SettingsViewModel : Screen
    {
        private readonly IConfigurationProvider configurationProvider;
        private readonly IAutostartProvider autostartProvider;

        public bool MinimizeToTray { get; set; }
        public bool CloseToTray { get; set; }
        public bool NotifyOfNewVersions { get; set; }
        public bool ObfuscateDeviceIDs { get; set; }

        public bool ShowTrayIconOnlyOnClose { get; set; }
        public bool ShowSynchronizedBalloon { get; set; }
        public bool ShowDeviceConnectivityBalloons { get; set; }

        public bool StartSyncThingAutomatically { get; set; }
        public bool SyncthingRunLowPriority { get; set; }
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

        public bool SyncthingUseCustomHome { get; set; }
        public string TraceVariables { get; set; }
        public bool SyncthingDenyUpgrade { get; set; }

        public SettingsViewModel(
            IConfigurationProvider configurationProvider,
            IAutostartProvider autostartProvider,
            IModelValidator<SettingsViewModel> validator)
            : base(validator)
        {
            this.configurationProvider = configurationProvider;
            this.autostartProvider = autostartProvider;

            var configuration = this.configurationProvider.Load();

            this.MinimizeToTray = configuration.MinimizeToTray;
            this.CloseToTray = configuration.CloseToTray;
            this.NotifyOfNewVersions = configuration.NotifyOfNewVersions;
            this.ObfuscateDeviceIDs = configuration.ObfuscateDeviceIDs;

            this.ShowTrayIconOnlyOnClose = configuration.ShowTrayIconOnlyOnClose;
            this.ShowSynchronizedBalloon = configuration.ShowSynchronizedBalloon;
            this.ShowDeviceConnectivityBalloons = configuration.ShowDeviceConnectivityBalloons;

            this.StartSyncThingAutomatically = configuration.StartSyncthingAutomatically;
            this.SyncthingRunLowPriority = configuration.SyncthingRunLowPriority;
            this.SyncThingAddress = configuration.SyncthingAddress;
            this.SyncThingApiKey = configuration.SyncthingApiKey;

            this.CanReadAutostart = this.autostartProvider.CanRead;
            this.CanWriteAutostart = this.autostartProvider.CanWrite;
            if (this.autostartProvider.CanRead)
            {
                var currentSetup = this.autostartProvider.GetCurrentSetup();
                this.StartOnLogon = currentSetup.AutoStart;
                this.StartMinimized = currentSetup.StartMinimized;
            }
            
            this.WatchedFolders = new BindableCollection<WatchedFolder>(configuration.Folders.Select(x => new WatchedFolder()
            {
                Folder = x.ID,
                IsSelected = x.IsWatched
            }));
            this.SyncthingUseCustomHome = configuration.SyncthingUseCustomHome;
            this.TraceVariables = configuration.SyncthingTraceFacilities;
            this.SyncthingDenyUpgrade = configuration.SyncthingDenyUpgrade;
        }

        protected override void OnValidationStateChanged(IEnumerable<string> changedProperties)
        {
            base.OnValidationStateChanged(changedProperties);
            this.NotifyOfPropertyChange(() => this.CanSave);
        }

        public bool CanSave
        {
            get { return !this.HasErrors; }
        }
        public void Save()
        {
            var configuration = this.configurationProvider.Load();

            configuration.MinimizeToTray = this.MinimizeToTray;
            configuration.CloseToTray = this.CloseToTray;
            configuration.NotifyOfNewVersions = this.NotifyOfNewVersions;
            configuration.ObfuscateDeviceIDs = this.ObfuscateDeviceIDs;

            configuration.ShowTrayIconOnlyOnClose = this.ShowTrayIconOnlyOnClose;
            configuration.ShowSynchronizedBalloon = this.ShowSynchronizedBalloon;
            configuration.ShowDeviceConnectivityBalloons = this.ShowDeviceConnectivityBalloons;

            configuration.StartSyncthingAutomatically = this.StartSyncThingAutomatically;
            configuration.SyncthingRunLowPriority = this.SyncthingRunLowPriority;
            configuration.SyncthingAddress = this.SyncThingAddress;
            configuration.SyncthingApiKey = this.SyncThingApiKey;

            if (this.autostartProvider.CanWrite)
            {
                var autostartConfig = new AutostartConfiguration() { AutoStart = this.StartOnLogon, StartMinimized = this.StartMinimized };
                this.autostartProvider.SetAutoStart(autostartConfig);
            }

            configuration.Folders = this.WatchedFolders.Select(x => new FolderConfiguration(x.Folder, x.IsSelected)).ToList();
            configuration.SyncthingUseCustomHome = this.SyncthingUseCustomHome;
            configuration.SyncthingTraceFacilities = String.IsNullOrWhiteSpace(this.TraceVariables) ? null : this.TraceVariables;
            configuration.SyncthingDenyUpgrade = this.SyncthingDenyUpgrade;

            this.configurationProvider.Save(configuration);
            this.RequestClose(true);
        }

        public void Cancel()
        {
            this.RequestClose(false);
        }
    }
}
