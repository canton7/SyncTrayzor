using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstallerRunner
{
    class Program
    {
        private const int ERROR_CANCELLED = 1223;

        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Must provide at least one command-line argument");
                return 1;
            }

            var startInfo = new ProcessStartInfo()
            {
                FileName = args[0],
                Arguments = String.Join(" ", args.Skip(1)),
                UseShellExecute = true,
                Verb = "runas",
            };

            try
            {
                Process.Start(startInfo);
            }
            catch (Win32Exception e)
            {
                if (e.NativeErrorCode == ERROR_CANCELLED)
                    return 2;
                else
                    return 3;
            }
            catch (Exception)
            {
                return 3;
            }

            return 0;
        }
    }
}
