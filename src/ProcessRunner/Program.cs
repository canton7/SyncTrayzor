using Mono.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessRunner
{
    class Program
    {
        private const int ERROR_CANCELLED = 1223;

        static int Main(string[] args)
        {
            bool showHelp = false;
            bool runas = false;
            string launch = null;
            bool shell = false;
            bool noWindow = false;

            var options = new OptionSet()
                .Add("help|h", "Show this help", v => showHelp = true)
                .Add("runas", "Run as Administrator (implies --shell)", v => runas = true)
                .Add("launch=", "Executable to run afterwards", v => launch = v)
                .Add("shell", "Set UseShellExecute = true", v => shell = true)
                .Add("nowindow", "Set CreateNoWindow = true", v => noWindow = true);

            var pivotIndex = Array.IndexOf(args, "--");
            if (pivotIndex < 0)
            {
                Console.Error.WriteLine("Must specify a command to run, see --help");
                return 1;
            }

            var remainder = args.Skip(pivotIndex + 1).ToList();
            var unknownArgs = options.Parse(args.Take(pivotIndex));

            if (unknownArgs.Count > 0)
            {
                Console.Error.WriteLine("Unknown argument {0}. See --help", unknownArgs[0]);
                return 1;
            }

            if (showHelp)
            {
                Console.WriteLine("Usage: ProcessRunner.exe [options] -- command");
                options.WriteOptionDescriptions(Console.Out);
                return 0;
            }

            if (remainder.Count == 0)
            {
                Console.Error.WriteLine("Must specify a command, See --help");
                return 1;
            }

            var startInfo = new ProcessStartInfo()
            {
                FileName = remainder[0],
                Arguments = String.Join(" ", remainder.Skip(1).Select(x => x.Contains(' ') ? String.Format("\"{0}\"", x) : x)),
                UseShellExecute = shell,
                CreateNoWindow = noWindow,
            };

            if (runas)
            {
                startInfo.Verb = "runas";
                startInfo.UseShellExecute = true;
            }

            try
            { 
                var process = Process.Start(startInfo);
                if (!String.IsNullOrWhiteSpace(launch))
                {
                    process.WaitForExit();
                    Process.Start(launch);
                }
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
