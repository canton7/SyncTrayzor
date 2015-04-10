using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Services
{
    public interface IProcessStartProvider
    {
        void Start(string filename);
        void Start(string filename, string arguments);
    }

    public class ProcessStartProvider : IProcessStartProvider
    {
        public void Start(string filename)
        {
            Process.Start(filename);
        }

        public void Start(string filename, string arguments)
        {
            Process.Start(filename, arguments);
        }
    }
}
