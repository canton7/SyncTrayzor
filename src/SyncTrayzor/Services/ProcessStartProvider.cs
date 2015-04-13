using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
        void StartElevatedDetached(string filename, string arguments);
    }

    public class ProcessStartProvider : IProcessStartProvider
    {
        private static readonly string installerRunner = "InstallerRunner.exe";
        private readonly string exeDir;

        public ProcessStartProvider(IAssemblyProvider assemblyProvider)
        {
            this.exeDir = assemblyProvider.Location;
        }

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

        public void StartElevatedDetached(string filename, string arguments)
        {
            var startInfo = new ProcessStartInfo()
            {
                FileName = Path.Combine(Path.GetDirectoryName(this.exeDir), installerRunner),
                Arguments = String.Format("\"{0}\" {1}", filename, arguments),
                CreateNoWindow = true,
                UseShellExecute = false,
            };

            Process.Start(startInfo);
        }
    }
}
