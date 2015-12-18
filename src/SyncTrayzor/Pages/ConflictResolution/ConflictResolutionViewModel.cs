using Stylet;
using SyncTrayzor.Services;
using SyncTrayzor.SyncThing;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.Pages.ConflictResolution
{
    public class ConflictResolutionViewModel : Screen
    {
        private readonly ISyncThingManager syncThingManager;
        private readonly IConflictFileManager conflictFileManager;
        private readonly IProcessStartProvider processStartProvider;

        private CancellationTokenSource loadingCts { get; set; }

        public bool IsLoading => this.loadingCts != null;
        public BindableCollection<ConflictViewModel> Conflicts { get; } = new BindableCollection<ConflictViewModel>();

        public ConflictViewModel SelectedConflict { get; set; }

        public ConflictResolutionViewModel(ISyncThingManager syncThingManager, IConflictFileManager conflictFileManager, IProcessStartProvider processStartProvider)
        {
            this.syncThingManager = syncThingManager;
            this.conflictFileManager = conflictFileManager;
            this.processStartProvider = processStartProvider;
        }

        protected override void OnInitialActivate()
        {
            this.Load();
        }

        private async void Load()
        {
            if (this.loadingCts != null)
            {
                this.loadingCts.Cancel();
                this.loadingCts = null;
            }

            this.loadingCts = new CancellationTokenSource();
            try
            {
                this.Conflicts.Clear();
                foreach (var folder in this.syncThingManager.Folders.FetchAll())
                {
                    try
                    {
                        await this.conflictFileManager.FindConflicts(folder.Path, this.loadingCts.Token).SubscribeAsync(x =>
                        {
                            this.Conflicts.Add(new ConflictViewModel(x, folder.FolderId));
                        });
                    }
                    catch (OperationCanceledException e) when (e.CancellationToken == this.loadingCts.Token)
                    { }
                }
            }
            finally
            {
                this.loadingCts = null;
            }
        }

        public void ConflictFileDoubleClick()
        {
            this.processStartProvider.ShowInExplorer(this.SelectedConflict.FilePath);
        }

        public void ChooseConflictFile(ConflictOptionViewModel conflictOption)
        {
            // Call into the service... Don't do this now for testing

            // The conflict will no longer exist, so remove it
            var correspondingVm = this.Conflicts.First(x => x.ConflictOptions.Contains(conflictOption));
            this.Conflicts.Remove(correspondingVm);
        }
    }
}
