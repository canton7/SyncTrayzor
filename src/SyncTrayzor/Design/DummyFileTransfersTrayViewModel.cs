using Stylet;
using SyncTrayzor.Pages;
using SyncTrayzor.SyncThing.EventWatcher;
using SyncTrayzor.SyncThing.TransferHistory;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Design
{
    public class DummyFileTransfersTrayViewModel
    {
        public BindableCollection<FileTransferViewModel> CompletedTransfers { get; private set; }
        public BindableCollection<FileTransferViewModel> InProgressTransfers { get; private set; }

        public bool HasCompletedTransfers
        {
            get { return this.CompletedTransfers.Count > 0; }
        }
        public bool HasInProgressTransfers
        {
            get { return this.InProgressTransfers.Count > 0; }
        }

        public string InConnectionRate { get; private set; }
        public string OutConnectionRate { get; private set; }

        public bool AnyTransfers { get; private set; }

        public DummyFileTransfersTrayViewModel()
        {
            this.CompletedTransfers = new BindableCollection<FileTransferViewModel>();
            this.InProgressTransfers = new BindableCollection<FileTransferViewModel>();

            var completedFileTransfer1 = new FileTransfer("folder", "path.pdf", ItemChangedItemType.File, ItemChangedActionType.Update);
            completedFileTransfer1.SetComplete(null);

            var completedFileTransfer2 = new FileTransfer("folder", "a really very long path that's far too long to sit on the page.h", ItemChangedItemType.File, ItemChangedActionType.Delete);
            completedFileTransfer2.SetComplete("Something went very wrong");

            //this.CompletedTransfers.Add(new FileTransferViewModel(completedFileTransfer1));
            //this.CompletedTransfers.Add(new FileTransferViewModel(completedFileTransfer2));

            var inProgressTransfer1 = new FileTransfer("folder", "path.txt", ItemChangedItemType.File, ItemChangedActionType.Update);
            inProgressTransfer1.SetDownloadProgress(5*1024*1024, 100*1024*1024);

            var inProgressTransfer2 = new FileTransfer("folder", "path", ItemChangedItemType.Folder, ItemChangedActionType.Update);

            this.InProgressTransfers.Add(new FileTransferViewModel(inProgressTransfer1));
            this.InProgressTransfers.Add(new FileTransferViewModel(inProgressTransfer2));

            this.InConnectionRate = "1.2MB";
            this.OutConnectionRate = "0.0MB";

            this.AnyTransfers = true;
        }
    }
}
