using NLog;
using SyncTrayzor.Services.Config;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace SyncTrayzor.Services.UpdateManagement
{
    public class PortableUpdateVariantHandler : IUpdateVariantHandler
    {
        private const string updateDownloadFileName = "SyncTrayzorUpdate-{0}.zip";
        private const string PortableInstallerName = "PortableInstaller.exe";

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IUpdateDownloader updateDownloader;
        private readonly IProcessStartProvider processStartProvider;
        private readonly IFilesystemProvider filesystem;
        private readonly IApplicationPathsProvider pathsProvider;
        private readonly IAssemblyProvider assemblyProvider;
        private readonly IApplicationState applicationState;

        private string extractedZipPath;

        public string VariantName => "portable";
        public bool RequiresUac => false;

        public bool CanAutoInstall { get; private set; }

        public PortableUpdateVariantHandler(
            IUpdateDownloader updateDownloader,
            IProcessStartProvider processStartProvider,
            IFilesystemProvider filesystem,
            IApplicationPathsProvider pathsProvider,
            IAssemblyProvider assemblyProvider,
            IApplicationState applicationState)
        {
            this.updateDownloader = updateDownloader;
            this.processStartProvider = processStartProvider;
            this.filesystem = filesystem;
            this.pathsProvider = pathsProvider;
            this.assemblyProvider = assemblyProvider;
            this.applicationState = applicationState;
        }

        public async Task<bool> TryHandleUpdateAvailableAsync(VersionCheckResults checkResult)
        {
            if (!String.IsNullOrWhiteSpace(checkResult.DownloadUrl) && !String.IsNullOrWhiteSpace(checkResult.Sha512sumDownloadUrl))
            {
                var zipPath = await this.updateDownloader.DownloadUpdateAsync(checkResult.DownloadUrl, checkResult.Sha512sumDownloadUrl, checkResult.NewVersion, updateDownloadFileName);
                if (zipPath == null)
                    return false;

                this.extractedZipPath = await this.ExtractDownloadedZip(zipPath);

                this.CanAutoInstall = true;

                // If we return false, the upgrade will be aborted
                return true;
            }
            else
            {
                // Can continue, but not auto-install
                this.CanAutoInstall = false;

                return true;
            }
        }

        public void AutoInstall(string pathToRestartApplication)
        {
            if (!this.CanAutoInstall)
                throw new InvalidOperationException("Auto-install not available");
            if (this.extractedZipPath == null)
                throw new InvalidOperationException("TryHandleUpdateAvailableAsync returned false: cannot call AutoInstall");

            var portableInstaller = Path.Combine(this.extractedZipPath, PortableInstallerName);

            if (!this.filesystem.FileExists(portableInstaller))
            {
                var e = new Exception($"Unable to find portable installer at {portableInstaller}");
                logger.Error(e);
                throw e;
            }

            // Need to move the portable installer out of its extracted archive, otherwise it won't be able to move the archive...

            var destPortableInstaller = Path.Combine(this.pathsProvider.UpdatesDownloadPath, PortableInstallerName);
            if (this.filesystem.FileExists(destPortableInstaller))
                this.filesystem.DeleteFile(destPortableInstaller);
            this.filesystem.MoveFile(portableInstaller, destPortableInstaller);

            var pid = Process.GetCurrentProcess().Id;

            // pathToRestartApplication IS ALREADY QUOTED: it's `"C:\Foo\Bar.exe"` or `"C:\Foo\Bar.exe" --minimized`. Portable installer
            // knows to look for either 4 or 5 arguments to take account of the fact that pathToRestartApplication may contain two bits 
            var args = $"\"{Path.GetDirectoryName(this.assemblyProvider.Location)}\" \"{this.extractedZipPath}\" {pid} {pathToRestartApplication}";

            this.processStartProvider.StartDetached(destPortableInstaller, args);

            this.applicationState.Shutdown();
        }

        private async Task<string> ExtractDownloadedZip(string zipPath)
        {
            var destinationDir = Path.Combine(Path.GetDirectoryName(zipPath), Path.GetFileNameWithoutExtension(zipPath));
            if (this.filesystem.DirectoryExists(destinationDir))
            {
                logger.Debug($"Extracted directory {destinationDir} already exists. Deleting...");
                this.filesystem.DeleteDirectory(destinationDir, true);
            }

            await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, destinationDir));

            // We expect a single folder inside the extracted dir, called e.g. SyncTrayzorPortable-x86
            var children = this.filesystem.GetDirectories(destinationDir);
            if (children.Length != 1)
                throw new Exception($"Expected 1 child in {destinationDir}, found {String.Join(", ", children)}");

            return children[0]; // Includes the path
        }
    }
}
