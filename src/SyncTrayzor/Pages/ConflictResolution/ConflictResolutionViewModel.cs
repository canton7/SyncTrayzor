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

        private CancellationTokenSource loadingCts { get; set; }

        public bool IsLoading => this.loadingCts != null;
        public BindableCollection<ConflictViewModel> Conflicts { get; } = new BindableCollection<ConflictViewModel>();

        public ConflictResolutionViewModel(ISyncThingManager syncThingManager, IConflictFileManager conflictFileManager)
        {
            this.syncThingManager = syncThingManager;
            this.conflictFileManager = conflictFileManager;
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
    }
}
