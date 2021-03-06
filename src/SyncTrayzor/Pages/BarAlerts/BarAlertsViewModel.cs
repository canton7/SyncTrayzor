using Stylet;
using SyncTrayzor.Pages.ConflictResolution;
using SyncTrayzor.Services;
using SyncTrayzor.Services.Config;
using SyncTrayzor.Syncthing;
using System;
using System.Collections.Generic;

namespace SyncTrayzor.Pages.BarAlerts
{
    public class BarAlertsViewModel : Conductor<IBarAlert>.Collection.AllActive
    {
        private readonly IAlertsManager alertsManager;
        private readonly ISyncthingManager syncthingManager;
        private readonly Func<ConflictResolutionViewModel> conflictResolutionViewModelFactory;
        private readonly Func<IntelXeGraphicsAlertViewModel> intelXeGraphicsAlertViewModelFactory;
        private readonly IWindowManager windowManager;
        private readonly IConfigurationProvider configurationProvider;
        private readonly GraphicsCardDetector graphicsCardDetector;

        public BarAlertsViewModel(
            IAlertsManager alertsManager,
            ISyncthingManager syncthingManager,
            Func<ConflictResolutionViewModel> conflictResolutionViewModelFactory,
            Func<IntelXeGraphicsAlertViewModel> intelXeGraphicsAlertViewModelFactory,
            IWindowManager windowManager,
            IConfigurationProvider configurationProvider,
            GraphicsCardDetector graphicsCardDetector)
        {
            this.alertsManager = alertsManager;
            this.syncthingManager = syncthingManager;
            this.conflictResolutionViewModelFactory = conflictResolutionViewModelFactory;
            this.intelXeGraphicsAlertViewModelFactory = intelXeGraphicsAlertViewModelFactory;
            this.windowManager = windowManager;
            this.configurationProvider = configurationProvider;
            this.graphicsCardDetector = graphicsCardDetector;
        }

        protected override void OnInitialActivate()
        {
            this.alertsManager.AlertsStateChanged += this.AlertsStateChanged;
            this.configurationProvider.ConfigurationChanged += this.AlertsStateChanged;
            this.Load();
        }

        private void AlertsStateChanged(object sender, EventArgs e)
        {
            this.Load();
        }

        private void Load()
        { 
            this.Items.Clear();

            var conflictedFilesCount = this.alertsManager.ConflictedFiles.Count;
            if (conflictedFilesCount > 0)
            {
                var vm = new ConflictsAlertViewModel(conflictedFilesCount);
                vm.OpenConflictResolverClicked += (o, e2) => this.OpenConflictResolver();
                this.Items.Add(vm);
            }

            var foldersWithFailedTransferFiles = this.alertsManager.FoldersWithFailedTransferFiles;
            if (foldersWithFailedTransferFiles.Count > 0)
            {
                var vm = new FailedTransfersAlertViewModel(foldersWithFailedTransferFiles);
                this.Items.Add(vm);
            }

            var pausedDeviceIds = this.alertsManager.PausedDeviceIdsFromMetering;
            if (pausedDeviceIds.Count > 0)
            {
                var pausedDeviceNames = new List<string>();
                foreach (var deviceId in pausedDeviceIds)
                {
                    if (this.syncthingManager.Devices.TryFetchById(deviceId, out var device))
                        pausedDeviceNames.Add(device.Name);
                }

                var vm = new PausedDevicesFromMeteringViewModel(pausedDeviceNames);
                this.Items.Add(vm);
            }

            var configuration = this.configurationProvider.Load();
            if (!configuration.DisableHardwareRendering && !configuration.HideIntelXeWarningMessage && this.graphicsCardDetector.IsIntelXe)
            {
                this.Items.Add(this.intelXeGraphicsAlertViewModelFactory());
            }
        }

        private void OpenConflictResolver()
        {
            var vm = this.conflictResolutionViewModelFactory();
            this.windowManager.ShowDialog(vm);
        }

        protected override void OnClose()
        {
            this.alertsManager.AlertsStateChanged -= this.AlertsStateChanged;
            this.configurationProvider.ConfigurationChanged -= this.AlertsStateChanged;
        }
    }
}
