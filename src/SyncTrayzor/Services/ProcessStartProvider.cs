using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        void StartDetached(string filename);
    }

    public class ProcessStartProvider : IProcessStartProvider
    {
        private const int ERROR_CANCELLED = 1223;

        public void Start(string filename)
        {
            Process.Start(filename);
        }

        public void Start(string filename, string arguments)
        {
            Process.Start(filename, arguments);
        }

        public void StartDetached(string filename)
        {
            var startInfo = new ProcessStartInfo()
            {
                FileName = "cmd.exe",
                Arguments = "/c start " + filename,
                CreateNoWindow = true,
                UseShellExecute = false,
            };

            Process.Start(startInfo);
        }
    }
}
