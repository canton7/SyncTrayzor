using Stylet;
using SyncTrayzor.Services;
using System;
using System.Linq;
using Pri.LongPath;
using System.Drawing;
using SyncTrayzor.Utils;

namespace SyncTrayzor.Pages.ConflictResolution
{
    public class ConflictViewModel : PropertyChangedBase
    {
        public ConflictSet ConflictSet { get; }

        public string FilePath => this.ConflictSet.File.FilePath;

        public string FileName => Path.GetFileName(this.ConflictSet.File.FilePath);

        public DateTime LastModified => this.ConflictSet.File.LastModified;

        public string Folder => Path.GetDirectoryName(this.ConflictSet.File.FilePath);

        public string InnerFolder => Path.GetFileName(this.Folder);

        public string FolderId { get; }

        public BindableCollection<ConflictOptionViewModel> ConflictOptions { get; }

        public Icon Icon { get; }
        

        public ConflictViewModel(ConflictSet conflictSet, string folderName)
        {
            this.ConflictSet = conflictSet;
            this.FolderId = folderName;

            this.ConflictOptions = new BindableCollection<ConflictOptionViewModel>(this.ConflictSet.Conflicts.Select(x => new ConflictOptionViewModel(x)));
            this.Icon = ShellTools.GetIcon(this.ConflictSet.File.FilePath, isFile: true);
        }
    }
}
