using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstallerRunner
{
    class Program
    {
        private const int ERROR_CANCELLED = 1223;

        static int Main(string[] argsIn)
        {
            var args = new List<string>(argsIn);

            string launch = null;
            var indexOfLaunch = args.IndexOf("-launch");
            if (indexOfLaunch > -1)
            {
                if (indexOfLaunch >= args.Count - 1)
                {
                    Console.Error.WriteLine("Must provide an argument to -launch");
                    return 1;
                }

                launch = args[indexOfLaunch + 1];
                args.RemoveAt(indexOfLaunch + 1);
                args.RemoveAt(indexOfLaunch);
            }

            if (args.Count == 0)
            {
                Console.Error.WriteLine("Must provide at least one command-line argument");
                return 1;
            }

            if (!File.Exists(args[0]))
            {
                Console.Error.WriteLine("Could not find {0}", args[0]);
                return 4;
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
                var process = Process.Start(startInfo);
                process.WaitForExit();

                if (!String.IsNullOrWhiteSpace(launch))
                    Process.Start(launch);
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
