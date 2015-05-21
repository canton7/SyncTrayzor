using Stylet;
using SyncTrayzor.Pages;
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

        public DummyFileTransfersTrayViewModel()
        {
            this.CompletedTransfers = new BindableCollection<FileTransferViewModel>();
            this.InProgressTransfers = new BindableCollection<FileTransferViewModel>();

            var completedFileTransfer1 = new FileTransfer("folder", "path.pdf");
            completedFileTransfer1.SetComplete();

            var completedFileTransfer2 = new FileTransfer("folder", "a really very long path that's far too long to sit on the page.h");
            completedFileTransfer2.SetComplete();

            this.CompletedTransfers.Add(new FileTransferViewModel(completedFileTransfer1));
            this.CompletedTransfers.Add(new FileTransferViewModel(completedFileTransfer2));

            var inProgressTransfer1 = new FileTransfer("folder", "path.txt");
            inProgressTransfer1.SetDownloadProgress(5*1024*1024, 100*1024*1024);
            this.InProgressTransfers.Add(new FileTransferViewModel(inProgressTransfer1));

            var inProgressTransfer2 = new FileTransfer("folder", "path.txt");
            this.InProgressTransfers.Add(new FileTransferViewModel(inProgressTransfer2));
        }
    }
}
