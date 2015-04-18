using FluentValidation;
using Stylet;
using SyncTrayzor.Localization;
using SyncTrayzor.Services;
using SyncTrayzor.Services.Config;
using SyncTrayzor.SyncThing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SyncTrayzor.Pages
{
    public class WatchedFolder
    {
        public string Folder { get; set; }
        public bool IsSelected { get; set; }
    }

    public static class EnvironmentalVariablesParser
    {
        public static string Format(EnvironmentalVariableCollection result)
        {
            return String.Join(" ", result.Select(x => String.Format("{0}={1}", x.Key, x.Value.Contains(' ') ? "\"" + x.Value + "\"" : x.Value)));
        }

        public static bool TryParse(string input, out EnvironmentalVariableCollection result)
        {
            if (String.IsNullOrWhiteSpace(input))
            {
                result = new EnvironmentalVariableCollection();
                return true;
            }

            result = null;

            // http://stackoverflow.com/a/4780801/1086121
            var parts = Regex.Split(input.Trim(), "(?<=^[^\"]+(?:\"[^\"]*\"[^\"]*)*) (?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

            var finalResult = new EnvironmentalVariableCollection();
            foreach (var part in parts)
            {
                var subParts = part.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (subParts.Length != 2)
                    return false;

                if (subParts[0].Contains('"'))
                    return false;

                if (subParts[1].StartsWith("\"") != subParts[1].EndsWith("\""))
                    return false;

                finalResult.Add(subParts[0], subParts[1].Trim('"'));
            }

            result = finalResult;
            return true;
        }
    }

    public class SettingsViewModelValidator : AbstractValidator<SettingsViewModel>
    {
        private const string apiKeyChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-";

        public SettingsViewModelValidator()
        {
            RuleFor(x => x.SyncThingAddress).NotEmpty().WithMessage(Localizer.Translate("SettingsView_Validation_NotShouldBeEmpty"));
            RuleFor(x => x.SyncThingAddress).Must(str =>
            {
                // URI seems to think https://http://something is valid...
                if (str.StartsWith("http:") || str.StartsWith("https:"))
                    return false;

                str = "https://" + str;
                Uri uri;
                return Uri.TryCreate(str, UriKind.Absolute, out uri) && uri.IsWellFormedOriginalString();
            }).WithMessage(Localizer.Translate("SettingsView_Validation_InvalidUrl"));

            RuleFor(x => x.SyncThingApiKey).NotEmpty().WithMessage(Localizer.Translate("SettingsView_Validation_NotShouldBeEmpty"));

            RuleFor(x => x.SyncThingEnvironmentalVariables).Must(str =>
            {
                EnvironmentalVariableCollection result;
                return EnvironmentalVariablesParser.TryParse(str, out result);
            }).WithMessage(Localizer.Translate("SettingsView_Validation_SyncthingEnvironmentalVariablesMustHaveFormat"));
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
        public bool UseComputerCulture { get; set; }

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
        public string SyncThingEnvironmentalVariables { get; set; }
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
            this.UseComputerCulture = configuration.UseComputerCulture;

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
            this.SyncThingEnvironmentalVariables = EnvironmentalVariablesParser.Format(configuration.SyncthingEnvironmentalVariables);
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
            configuration.UseComputerCulture = this.UseComputerCulture;

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

            EnvironmentalVariableCollection envVars;
            EnvironmentalVariablesParser.TryParse(this.SyncThingEnvironmentalVariables, out envVars);
            configuration.SyncthingEnvironmentalVariables = envVars;

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
