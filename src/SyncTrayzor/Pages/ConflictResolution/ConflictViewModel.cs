using Stylet;
using SyncTrayzor.Services.Conflicts;
using System;
using System.Linq;
using Pri.LongPath;
using SyncTrayzor.Utils;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows;

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

        public string FolderLabel { get; }

        public BindableCollection<ConflictOptionViewModel> ConflictOptions { get; }

        public ImageSource Icon { get; }

        public string Size => FormatUtils.BytesToHuman(this.ConflictSet.File.SizeBytes, 1);

        public bool IsSelected { get; set; }
        

        public ConflictViewModel(ConflictSet conflictSet, string folderName)
        {
            this.ConflictSet = conflictSet;
            this.FolderLabel = folderName;

            this.ConflictOptions = new BindableCollection<ConflictOptionViewModel>(this.ConflictSet.Conflicts.Select(x => new ConflictOptionViewModel(x)));

            // These bindings aren't called lazilly, so don't bother being lazy
            using (var icon = ShellTools.GetIcon(this.ConflictSet.File.FilePath, isFile: true))
            {
                if (icon != null)
                {
                    var bs = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    bs.Freeze();
                    this.Icon = bs;
                }
            }
        }
    }
}
