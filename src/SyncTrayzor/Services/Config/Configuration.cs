using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace SyncTrayzor.Services.Config
{
    [XmlRoot("Configuration")]
    public class Configuration
    {
        public const int CurrentVersion = 10;
        public const double DefaultSyncthingConsoleHeight = 100;

        [XmlAttribute("Version")]
        public int Version
        {
            get => CurrentVersion;
            set
            {
                if (CurrentVersion != value)
                    throw new InvalidOperationException($"Can't deserialize config of version {value} (expected {CurrentVersion})");
            }
        }

        public bool ShowTrayIconOnlyOnClose { get; set; }
        public bool MinimizeToTray { get; set; }
        public bool CloseToTray { get; set; }
        public bool ShowDeviceConnectivityBalloons { get; set; }
        public bool ShowDeviceOrFolderRejectedBalloons { get; set; }
        public bool ShowSynchronizedBalloonEvenIfNothingDownloaded { get; set; }
        public string SyncthingAddress { get; set; }
        public bool StartSyncthingAutomatically { get; set; }

        [XmlArrayItem("SyncthingCommandLineFlag")]
        public List<string> SyncthingCommandLineFlags { get; set; }
        public EnvironmentalVariableCollection SyncthingEnvironmentalVariables { get; set; }
        public bool SyncthingDenyUpgrade { get; set; }
        public SyncthingPriorityLevel SyncthingPriorityLevel { get; set; }

        [XmlArrayItem("Folder")]
        public List<FolderConfiguration> Folders { get; set; }

        public bool NotifyOfNewVersions { get; set; }
        public bool ObfuscateDeviceIDs { get; set; }

        [XmlIgnore]
        public Version LatestNotifiedVersion { get; set; }
        [XmlElement("LatestNotifiedVersion")]
        public string LatestNotifiedVersionRaw
        {
            get => this.LatestNotifiedVersion?.ToString();
            set => this.LatestNotifiedVersion = value == null ? null : new Version(value);
        }

        public bool UseComputerCulture { get; set; }
        public double SyncthingConsoleHeight { get; set; }
        public WindowPlacement WindowPlacement { get; set; }
        public double SyncthingWebBrowserZoomLevel { get; set; }
        public int LastSeenInstallCount { get; set; }
        public string SyncthingCustomPath { get; set; }
        public string SyncthingCustomHomePath { get; set; }
        public bool DisableHardwareRendering { get; set; }
        public bool EnableFailedTransferAlerts { get; set; }
        public bool EnableConflictFileMonitoring { get; set; }

        [XmlArrayItem("DebugFacility")]
        public List<string> SyncthingDebugFacilities { get; set; }

        public bool ConflictResolverDeletesToRecycleBin { get; set; }
        public bool PauseDevicesOnMeteredNetworks { get; set; }
        public bool HaveDonated { get; set; }
        public IconAnimationMode IconAnimationMode { get; set; }
        public string OpenFolderCommand { get; set; }
        public string ShowFileInFolderCommand { get; set; }
        public LogLevel LogLevel { get; set; }

        public Configuration()
        {
            // Default configuration is for a portable setup.

            this.ShowTrayIconOnlyOnClose = false;
            this.MinimizeToTray = false;
            this.CloseToTray = true;
            this.ShowSynchronizedBalloonEvenIfNothingDownloaded = false;
            this.ShowDeviceConnectivityBalloons = true;
            this.ShowDeviceOrFolderRejectedBalloons = true;
            this.SyncthingAddress = "localhost:8384";
            this.StartSyncthingAutomatically = true;
            this.SyncthingCommandLineFlags = new List<string>();
            this.SyncthingEnvironmentalVariables = new EnvironmentalVariableCollection();
            this.SyncthingDenyUpgrade = false;
            this.SyncthingPriorityLevel = SyncthingPriorityLevel.Normal;
            this.Folders = new List<FolderConfiguration>();
            this.NotifyOfNewVersions = true;
            this.ObfuscateDeviceIDs = true;
            this.LatestNotifiedVersion = null;
            this.UseComputerCulture = true;
            this.SyncthingConsoleHeight = DefaultSyncthingConsoleHeight;
            this.WindowPlacement = null;
            this.SyncthingWebBrowserZoomLevel = 0;
            this.LastSeenInstallCount = 0;
            this.SyncthingCustomPath = null;
            this.SyncthingCustomHomePath = null;
            this.DisableHardwareRendering = false;
            this.EnableFailedTransferAlerts = true;
            this.EnableConflictFileMonitoring = true;
            this.SyncthingDebugFacilities = new List<string>();
            this.ConflictResolverDeletesToRecycleBin = true;
            this.PauseDevicesOnMeteredNetworks = true;
            this.HaveDonated = false;
            this.IconAnimationMode = IconAnimationMode.DataTransferring;
            this.OpenFolderCommand = "explorer.exe \"{0}\"";
            this.ShowFileInFolderCommand = "explorer.exe /select, \"{0}\"";
            this.LogLevel = LogLevel.Info;
        }

        public Configuration(Configuration other)
        {
            this.ShowTrayIconOnlyOnClose = other.ShowTrayIconOnlyOnClose;
            this.MinimizeToTray = other.MinimizeToTray;
            this.CloseToTray = other.CloseToTray;
            this.ShowSynchronizedBalloonEvenIfNothingDownloaded = other.ShowSynchronizedBalloonEvenIfNothingDownloaded;
            this.ShowDeviceConnectivityBalloons = other.ShowDeviceConnectivityBalloons;
            this.ShowDeviceOrFolderRejectedBalloons = other.ShowDeviceOrFolderRejectedBalloons;
            this.SyncthingAddress = other.SyncthingAddress;
            this.StartSyncthingAutomatically = other.StartSyncthingAutomatically;
            this.SyncthingCommandLineFlags = other.SyncthingCommandLineFlags;
            this.SyncthingEnvironmentalVariables = other.SyncthingEnvironmentalVariables;
            this.SyncthingDenyUpgrade = other.SyncthingDenyUpgrade;
            this.SyncthingPriorityLevel = other.SyncthingPriorityLevel;
            this.Folders = other.Folders.Select(x => new FolderConfiguration(x)).ToList();
            this.NotifyOfNewVersions = other.NotifyOfNewVersions;
            this.ObfuscateDeviceIDs = other.ObfuscateDeviceIDs;
            this.LatestNotifiedVersion = other.LatestNotifiedVersion;
            this.UseComputerCulture = other.UseComputerCulture;
            this.SyncthingConsoleHeight = other.SyncthingConsoleHeight;
            this.WindowPlacement = other.WindowPlacement;
            this.SyncthingWebBrowserZoomLevel = other.SyncthingWebBrowserZoomLevel;
            this.LastSeenInstallCount = other.LastSeenInstallCount;
            this.SyncthingCustomPath = other.SyncthingCustomPath;
            this.SyncthingCustomHomePath = other.SyncthingCustomHomePath;
            this.DisableHardwareRendering = other.DisableHardwareRendering;
            this.EnableFailedTransferAlerts = other.EnableFailedTransferAlerts;
            this.EnableConflictFileMonitoring = other.EnableConflictFileMonitoring;
            this.SyncthingDebugFacilities = other.SyncthingDebugFacilities;
            this.ConflictResolverDeletesToRecycleBin = other.ConflictResolverDeletesToRecycleBin;
            this.PauseDevicesOnMeteredNetworks = other.PauseDevicesOnMeteredNetworks;
            this.HaveDonated = other.HaveDonated;
            this.IconAnimationMode = other.IconAnimationMode;
            this.OpenFolderCommand = other.OpenFolderCommand;
            this.ShowFileInFolderCommand = other.ShowFileInFolderCommand;
            this.LogLevel = other.LogLevel;
        }

        public override string ToString()
        {
            return $"<Configuration ShowTrayIconOnlyOnClose={this.ShowTrayIconOnlyOnClose} MinimizeToTray={this.MinimizeToTray} CloseToTray={this.CloseToTray} " +
                $"ShowDeviceConnectivityBalloons={this.ShowDeviceConnectivityBalloons} ShowDeviceOrFolderRejectedBalloons={this.ShowDeviceOrFolderRejectedBalloons} " +
                $"SyncthingAddress={this.SyncthingAddress} StartSyncthingAutomatically={this.StartSyncthingAutomatically} " +
                $"SyncthingCommandLineFlags=[{String.Join(",", this.SyncthingCommandLineFlags)}] " +
                $"SyncthingEnvironmentalVariables=[{String.Join(" ", this.SyncthingEnvironmentalVariables)}] " +
                $"SyncthingDenyUpgrade={this.SyncthingDenyUpgrade} SyncthingPriorityLevel={this.SyncthingPriorityLevel} " +
                $"Folders=[{String.Join(", ", this.Folders)}] NotifyOfNewVersions={this.NotifyOfNewVersions} LatestNotifiedVersion={this.LatestNotifiedVersion} " +
                $"ObfuscateDeviceIDs={this.ObfuscateDeviceIDs} UseComputerCulture={this.UseComputerCulture} SyncthingConsoleHeight={this.SyncthingConsoleHeight} WindowPlacement={this.WindowPlacement} " +
                $"SyncthingWebBrowserZoomLevel={this.SyncthingWebBrowserZoomLevel} LastSeenInstallCount={this.LastSeenInstallCount} SyncthingCustomPath={this.SyncthingCustomPath} " +
                $"SyncthingCustomHomePath={this.SyncthingCustomHomePath} ShowSynchronizedBalloonEvenIfNothingDownloaded={this.ShowSynchronizedBalloonEvenIfNothingDownloaded} " +
                $"DisableHardwareRendering={this.DisableHardwareRendering} EnableFailedTransferAlerts={this.EnableFailedTransferAlerts} " +
                $"EnableConflictFileMonitoring={this.EnableConflictFileMonitoring} SyncthingDebugFacilities=[{String.Join(",", this.SyncthingDebugFacilities)}] "+
                $"ConflictResolverDeletesToRecycleBin={this.ConflictResolverDeletesToRecycleBin} PauseDevicesOnMeteredNetworks={this.PauseDevicesOnMeteredNetworks} "+
                $"HaveDonated={this.HaveDonated} IconAnimationMode={this.IconAnimationMode} OpenFolderCommand={this.OpenFolderCommand} ShowFileInFolderCommand={this.ShowFileInFolderCommand}" +
                $"LogLevel={this.LogLevel}>";
        }
    }
}
