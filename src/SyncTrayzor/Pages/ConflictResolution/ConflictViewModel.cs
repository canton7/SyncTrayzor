using Stylet;
using SyncTrayzor.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Pages.ConflictResolution
{
    public class ConflictViewModel : PropertyChangedBase
    {
        public ConflictSet ConflictSet { get; }

        public string FileName => this.ConflictSet.File.FileName;

        public ConflictViewModel(ConflictSet conflictSet)
        {
            this.ConflictSet = conflictSet;
        }
    }
}
