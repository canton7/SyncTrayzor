using NLog;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Services.UpdateManagement
{
    public interface IInstallerCertificateVerifier
    {
        Stream VerifySha1sum(string filePath);
        bool VerifyUpdate(string filePath, Stream sha1sumFile);
    }

    public class InstallerCertificateVerifier : IInstallerCertificateVerifier
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private const string certificateName = "SyncTrayzor.Resources.synctrayzor_releases_cert.asc";

        private readonly IAssemblyProvider assemblyProvider;

        public InstallerCertificateVerifier(IAssemblyProvider assemblyProvider)
        {
            this.assemblyProvider = assemblyProvider;
        }

        private Stream LoadCertificate()
        {
            return this.assemblyProvider.GetManifestResourceStream(certificateName);
        }

        public Stream VerifySha1sum(string filePath)
        {
            using (var file = File.OpenRead(filePath))
            using (var certificate = this.LoadCertificate())
            {
                return PgpClearsignUtilities.ReadAndVerifyFile(file, certificate);
            }
        }

        public bool VerifyUpdate(string filePath, Stream sha1sumFile)
        {
            using (var hashAlgorithm = new SHA1Managed())
            using (var file = File.OpenRead(filePath))
            {
                return ChecksumFileUtilities.ValidateChecksum(hashAlgorithm, sha1sumFile, Path.GetFileName(filePath), file);
            }
        }
    }
}
