using System;
using System.IO;
using System.Reflection;

namespace SyncTrayzor.Services
{
    public interface IAssemblyProvider
    {
        Version Version { get; }
        Version FullVersion { get; }
        string Location { get; }
        ProcessorArchitecture ProcessorArchitecture { get; }
        Stream GetManifestResourceStream(string path);
    }

    public class AssemblyProvider : IAssemblyProvider
    {
        private readonly Assembly assembly;

        public AssemblyProvider()
        {
            this.assembly = Assembly.GetExecutingAssembly();

            // Don't include the revision in this version
            var version = this.assembly.GetName().Version;
            this.Version = new Version(version.Major, version.Minor, version.Build);
        }

        public Version Version { get; }

        public Version FullVersion => this.assembly.GetName().Version;

        public string Location => this.assembly.Location;

        public ProcessorArchitecture ProcessorArchitecture => this.assembly.GetName().ProcessorArchitecture;

        public Stream GetManifestResourceStream(string path)
        {
            return this.assembly.GetManifestResourceStream(path);
        }
    }
}
