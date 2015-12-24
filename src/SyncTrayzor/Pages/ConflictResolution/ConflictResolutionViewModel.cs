using Stylet;
using SyncTrayzor.Services;
using SyncTrayzor.SyncThing;
using SyncTrayzor.Utils;
using System;
using System.Linq;
using System.Threading;
using System.Collections.Specialized;
using System.IO;
using SyncTrayzor.Localization;
using System.Windows;
using SyncTrayzor.Properties;

namespace SyncTrayzor.Pages.ConflictResolution
{
    public class ConflictResolutionViewModel : Screen
    {
        private readonly ISyncThingManager syncThingManager;
        private readonly IConflictFileManager conflictFileManager;
        private readonly IProcessStartProvider processStartProvider;
        private readonly IWindowManager windowManager;

        private CancellationTokenSource loadingCts { get; set; }

        public bool IsLoading => this.loadingCts != null;
        public BindableCollection<ConflictViewModel> Conflicts { get; } = new BindableCollection<ConflictViewModel>();
        public bool IsLoadingAndNoConflictsFound => this.IsLoading && this.Conflicts.Count == 0;
        public bool HasFinishedLoadingAndNoConflictsFound => !this.IsSyncthingStopped && !this.IsLoading && this.Conflicts.Count == 0;
        public bool IsSyncthingStopped { get; private set; }

        public ConflictViewModel SelectedConflict { get; set; }

        public ConflictResolutionViewModel(
            ISyncThingManager syncThingManager,
            IConflictFileManager conflictFileManager,
            IProcessStartProvider processStartProvider,
            IWindowManager windowManager)
        {
            this.syncThingManager = syncThingManager;
            this.conflictFileManager = conflictFileManager;
            this.processStartProvider = processStartProvider;
            this.windowManager = windowManager;

            this.Conflicts.CollectionChanged += (o, e) =>
            {
                if ((e.Action == NotifyCollectionChangedAction.Add && (e.OldItems?.Count ?? 0) == 0) ||
                    (e.Action == NotifyCollectionChangedAction.Remove && (e.NewItems?.Count ?? 0) == 0) ||
                    (e.Action == NotifyCollectionChangedAction.Reset))
                {
                    this.NotifyOfPropertyChange(nameof(this.Conflicts));
                    this.NotifyOfPropertyChange(nameof(this.IsLoadingAndNoConflictsFound));
                    this.NotifyOfPropertyChange(nameof(this.HasFinishedLoadingAndNoConflictsFound));
                }
            };
        }

        private void SyncThingDataLoaded(object sender, EventArgs e)
        {
            this.IsSyncthingStopped = false;
            this.Load();
        }

        protected override void OnInitialActivate()
        {
            if (this.syncThingManager.State != SyncThingState.Running || !this.syncThingManager.IsDataLoaded)
            {
                this.IsSyncthingStopped = true;
                this.syncThingManager.DataLoaded += this.SyncThingDataLoaded;
            }
            else
            {
                this.IsSyncthingStopped = false;
                this.Load();
            }
        }

        protected override void OnClose()
        {
            this.loadingCts?.Cancel();
            this.syncThingManager.DataLoaded -= this.SyncThingDataLoaded;
        }

        private async void Load()
        {
            if (this.loadingCts != null)
            {
                this.loadingCts.Cancel();
                this.loadingCts = null;
            }

            this.loadingCts = new CancellationTokenSource();
            var ct = this.loadingCts.Token;
            try
            {
                this.Conflicts.Clear();
                foreach (var folder in this.syncThingManager.Folders.FetchAll())
                {
                    try
                    {
                        await this.conflictFileManager.FindConflicts(folder.Path, ct).SubscribeAsync(x =>
                        {
                            this.Conflicts.Add(new ConflictViewModel(x, folder.FolderId));
                        });
                    }
                    catch (OperationCanceledException e) when (e.CancellationToken == ct)
                    { }
                }
            }
            finally
            {
                this.loadingCts = null;
            }
        }

        public void Cancel()
        {
            this.loadingCts.Cancel();
        }

        public void ConflictFileDoubleClick()
        {
            this.processStartProvider.ShowInExplorer(this.SelectedConflict.FilePath);
        }

        public void ChooseOriginal(ConflictViewModel conflict)
        {
            if (!this.ResolveConflict(this.SelectedConflict.ConflictSet, conflict.ConflictSet.File.FilePath))
                return;

            // The conflict will no longer exist, so remove it
            this.Conflicts.Remove(conflict);
        }

        public void ChooseConflictFile(ConflictOptionViewModel conflictOption)
        {
            // Call into the service... Don't do this now for testing
            if (!this.ResolveConflict(this.SelectedConflict.ConflictSet, conflictOption.ConflictOption.FilePath))
                return;

            // The conflict will no longer exist, so remove it
            var correspondingVm = this.Conflicts.First(x => x.ConflictOptions.Contains(conflictOption));
            this.Conflicts.Remove(correspondingVm);
        }

        private bool ResolveConflict(ConflictSet conflictSet, string filePath)
        {
            try
            {
                this.conflictFileManager.ResolveConflict(conflictSet, filePath);
                return true;
            }
            catch (IOException e)
            {
                this.windowManager.ShowMessageBox(
                    Localizer.F(Resources.ConflictResolutionView_Dialog_Failed_Message, e.Message),
                    Resources.ConflictResolutionView_Dialog_Failed_Title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return false;
            }
        }

        public void Close()
        {
            this.RequestClose(true);
        }
    }
}
