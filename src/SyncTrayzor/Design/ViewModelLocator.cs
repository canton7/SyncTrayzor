using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Design
{
    public class ViewModelLocator
    {
        public DummyFileTransfersTrayViewModel FileTransfersTrayViewModel
        {
            get { return new DummyFileTransfersTrayViewModel(); }
        }
    }
}
