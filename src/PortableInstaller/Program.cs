using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortableInstaller
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("You should not invoke this executable directly. It is used as part of the automatic upgrade process for portable installations.");
                Console.ReadKey();
                return 0;
            }
            try
            {
                var destinationPath = args[0];
                var sourcePath = args[1];
                var waitForPid = Int32.Parse(args[2]);
                var pathToRestartApplication = args[3];

                Console.WriteLine("Waiting for process to exit...");
                try
                {
                    using (var process = Process.GetProcessById(waitForPid))
                    {
                        if (!process.WaitForExit(5000))
                            throw new Exception($"SyncTrayzor process with PID {waitForPid} did not exit after 5 seconds");
                    }
                }
                catch (ArgumentException) // It wasn't running to start with. Coolio
                { }

                // By default ou CWD is the destinationPath, which locks it
                Directory.SetCurrentDirectory(Path.GetDirectoryName(destinationPath));

                var destinationExists = Directory.Exists(destinationPath);

                if (!Directory.Exists(sourcePath))
                {
                    Console.WriteLine($"Unable to find source path {sourcePath}");
                    return 1;
                }

                string movedDestinationPath = null;
                if (destinationExists)
                {
                    movedDestinationPath = GenerateBackupDestinationPath(destinationPath);
                    Console.WriteLine($"Moving {destinationPath} to {movedDestinationPath}");
                    Directory.Move(destinationPath, movedDestinationPath);
                }

                Console.WriteLine($"Moving {sourcePath} to {destinationPath}");
                Directory.Move(sourcePath, destinationPath);

                if (destinationExists)
                {
                    var sourceDataFolder = Path.Combine(movedDestinationPath, "data");
                    if (Directory.Exists(sourceDataFolder))
                    {
                        var destDataFolder = Path.Combine(destinationPath, "data");
                        Console.WriteLine($"Copying data folder {sourceDataFolder} to {destDataFolder}...");
                        DirectoryCopy(sourceDataFolder, destDataFolder);
                    }
                    else
                    {
                        Console.WriteLine($"Could not find source data folder {sourceDataFolder}, so not copying");
                    }

                    var sourceInstallCount = Path.Combine(movedDestinationPath, "InstallCount.txt");
                    var destInstallCount = Path.Combine(destinationPath, "InstallCount.txt");
                    if (File.Exists(sourceInstallCount))
                    {
                        var installCount = Int32.Parse(File.ReadAllText(sourceInstallCount).Trim());
                        Console.WriteLine($"Increasing install count to {installCount + 1} from {sourceInstallCount} to {destInstallCount}");
                        File.WriteAllText(destInstallCount, (installCount + 1).ToString());
                    }
                    else
                    {
                        Console.WriteLine($"{sourceInstallCount} doesn't exist, so setting installCount to 1 in {destInstallCount}");
                        File.WriteAllText(destInstallCount, "1");
                    }

                    Console.WriteLine($"Deleting {movedDestinationPath}");
                    Directory.Delete(movedDestinationPath, true);
                }

                Console.WriteLine($"Restarting application {pathToRestartApplication}");
                Process.Start(pathToRestartApplication);

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.WriteLine($"--- An error occurred ---");
                Console.WriteLine($"{e.GetType().Name}: {e.Message}");
                Console.WriteLine();
                Console.WriteLine("The upgrade failed to complete successfully. Sorry about that.");
                Console.WriteLine("Press any key to continue");
                Console.ReadKey();
                return 2;
            }
        }

        private static string GenerateBackupDestinationPath(string path)
        {
            for (int i = 1; i < 1000; i++)
            {
                var tempPath = $"{path}_{i}";
                if (!Directory.Exists(tempPath) && !File.Exists(tempPath))
                {
                    return tempPath;
                }
            }

            throw new Exception("Count not generate a backup path");
        }

        // From https://msdn.microsoft.com/en-us/library/bb762914%28v=vs.110%29.aspx
        private static void DirectoryCopy(string sourceDirName, string destDirName)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, true); // Overwrite
            }

            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, temppath);
            }
        }
    }
}
