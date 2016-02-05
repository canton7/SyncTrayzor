using Stylet;
using SyncTrayzor.Services.Conflicts;
using SyncTrayzor.Syncthing;
using SyncTrayzor.Utils;
using System;
using System.Linq;
using System.Threading;
using System.Collections.Specialized;
using System.IO;
using SyncTrayzor.Localization;
using System.Windows;
using SyncTrayzor.Properties;
using SyncTrayzor.Services;
using SyncTrayzor.Services.Config;
using System.Reactive.Threading.Tasks;
using System.Reactive.Linq;
using System.Windows.Threading;

namespace SyncTrayzor.Pages.ConflictResolution
{
    public class ConflictResolutionViewModel : Screen
    {
        private readonly ISyncthingManager syncthingManager;
        private readonly IConflictFileManager conflictFileManager;
        private readonly IProcessStartProvider processStartProvider;
        private readonly IConflictFileWatcher conflictFileWatcher;
        private readonly IWindowManager windowManager;
        private readonly IConfigurationProvider configurationProvider;

        private bool wasConflictFileWatcherEnabled;

        private CancellationTokenSource loadingCts { get; set; }

        public bool IsLoading => this.loadingCts != null;
        public BindableCollection<ConflictViewModel> Conflicts { get; } = new BindableCollection<ConflictViewModel>();
        public bool IsLoadingAndNoConflictsFound => this.IsLoading && this.Conflicts.Count == 0;
        public bool HasFinishedLoadingAndNoConflictsFound => !this.IsSyncthingStopped && !this.IsLoading && this.Conflicts.Count == 0;
        public bool IsSyncthingStopped { get; private set; }

        public bool DeleteToRecycleBin { get; set; }

        public ConflictViewModel SelectedConflict { get; set; }

        public ConflictResolutionViewModel(
            ISyncthingManager syncthingManager,
            IConflictFileManager conflictFileManager,
            IProcessStartProvider processStartProvider,
            IConflictFileWatcher conflictFileWatcher,
            IWindowManager windowManager,
            IConfigurationProvider configurationProvider)
        {
            this.syncthingManager = syncthingManager;
            this.conflictFileManager = conflictFileManager;
            this.processStartProvider = processStartProvider;
            this.conflictFileWatcher = conflictFileWatcher;
            this.configurationProvider = configurationProvider;
            this.windowManager = windowManager;

            this.DeleteToRecycleBin = this.configurationProvider.Load().ConflictResolverDeletesToRecycleBin;
            this.Bind(s => s.DeleteToRecycleBin, (o, e) => this.configurationProvider.AtomicLoadAndSave(c => c.ConflictResolverDeletesToRecycleBin = e.NewValue));

            this.Conflicts.CollectionChanged += (o, e) =>
            {
                if ((e.Action == NotifyCollectionChangedAction.Add && (e.OldItems?.Count ?? 0) == 0) ||
                    (e.Action == NotifyCollectionChangedAction.Remove && (e.NewItems?.Count ?? 0) == 0) ||
                    (e.Action == NotifyCollectionChangedAction.Reset))
                {
                    this.NotifyOfPropertyChange(nameof(this.Conflicts));
                    this.NotifyOfPropertyChange(nameof(this.IsLoadingAndNoConflictsFound));
                    this.NotifyOfPropertyChange(nameof(this.HasFinishedLoadingAndNoConflictsFound));

                    if (this.SelectedConflict == null && this.Conflicts.Count > 0)
                        this.SelectedConflict = this.Conflicts[0];
                }
            };
        }

        private void SyncthingDataLoaded(object sender, EventArgs e)
        {
            this.IsSyncthingStopped = false;
            this.Load();
        }

        protected override void OnInitialActivate()
        {
            // This is hacky
            this.wasConflictFileWatcherEnabled = this.conflictFileWatcher.IsEnabled;
            this.conflictFileWatcher.IsEnabled = false;

            if (this.syncthingManager.State != SyncthingState.Running || !this.syncthingManager.IsDataLoaded)
            {
                this.IsSyncthingStopped = true;
                this.syncthingManager.DataLoaded += this.SyncthingDataLoaded;
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
            if (this.wasConflictFileWatcherEnabled)
                this.conflictFileWatcher.IsEnabled = true;
            this.syncthingManager.DataLoaded -= this.SyncthingDataLoaded;
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
                foreach (var folder in this.syncthingManager.Folders.FetchAll())
                {
                    try
                    {
                        await this.conflictFileManager.FindConflicts(folder.Path)
                            .ObserveOnDispatcher(DispatcherPriority.Background)
                            .ForEachAsync(conflict => this.Conflicts.Add(new ConflictViewModel(conflict, folder.FolderId)), ct);
                    }
                    catch (OperationCanceledException) { }
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

        public void ListViewDoubleClick(object sender, RoutedEventArgs e)
        {
            // Check that we were called on a row, not on a header
            if ((e.OriginalSource as FrameworkElement)?.DataContext is ConflictViewModel)
                this.ShowFileInFolder();
        }

        public void ShowFileInFolder()
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
            if (!this.ResolveConflict(this.SelectedConflict.ConflictSet, conflictOption.ConflictOption.FilePath))
                return;

            // The conflict will no longer exist, so remove it
            var correspondingVm = this.Conflicts.First(x => x.ConflictOptions.Contains(conflictOption));
            this.Conflicts.Remove(correspondingVm);
        }

        private bool ResolveConflict(ConflictSet conflictSet, string filePath)
        {
            // This can happen e.g. if the file chosen no longer exists
            try
            {
                this.conflictFileManager.ResolveConflict(conflictSet, filePath, this.DeleteToRecycleBin);
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
