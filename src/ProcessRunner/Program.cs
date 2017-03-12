using Mono.Options;
using SyncTrayzor.Utils;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

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
                ShowHelp(options);
                return 1;
            }

            var remainder = args.Skip(pivotIndex + 1).ToList();
            var unknownArgs = options.Parse(args.Take(pivotIndex));

            if (showHelp)
            {
                ShowHelp(options);
                return 1;
            }

            if (unknownArgs.Count > 0)
            {
                Console.Error.WriteLine("Unknown argument {0}. See --help", unknownArgs[0]);
                return 1;
            }

            if (remainder.Count == 0)
            {
                Console.Error.WriteLine("Must specify a command, See --help");
                return 1;
            }

            var startInfo = new ProcessStartInfo()
            {
                FileName = remainder[0],
                Arguments = StringExtensions.JoinCommandLine(remainder.Skip(1)),
                UseShellExecute = shell,
                CreateNoWindow = noWindow,
            };

            if (runas)
            {
                startInfo.Verb = "runas";
                startInfo.UseShellExecute = true;
            }

            string launchFilename = null;
            string launchArguments = null;

            if (!String.IsNullOrWhiteSpace(launch))
            {
                var parts = StringExtensions.SplitCommandLine(launch);
                launchFilename = parts.First();
                launchArguments = StringExtensions.JoinCommandLine(parts.Skip(1));
            }

            try
            { 
                var process = Process.Start(startInfo);
                if (!String.IsNullOrWhiteSpace(launchFilename))
                {
                    process.WaitForExit();
                    var launchStartInfo = new ProcessStartInfo()
                    {
                        FileName = launchFilename,
                        Arguments = launchArguments,
                    };
                    Process.Start(launchStartInfo);
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

        private static void ShowHelp(OptionSet options)
        {
            Console.WriteLine("Usage: ProcessRunner.exe [options] -- command");
            options.WriteOptionDescriptions(Console.Out);
        }
    }
}
