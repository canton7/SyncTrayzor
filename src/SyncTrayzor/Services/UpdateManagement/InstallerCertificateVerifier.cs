using NLog;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Services.UpdateManagement
{
    public interface IInstallerCertificateVerifier
    {
        bool Verify(string filePath, FileStream openStream);
    }

    public class InstallerCertificateVerifier : IInstallerCertificateVerifier
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private string certThumbprint;

        public InstallerCertificateVerifier(IAssemblyProvider assemblyProvider)
        {
            using (var certStream = assemblyProvider.GetManifestResourceStream("SyncTrayzor.Resources.SyncTrayzorCA.cer"))
            {
                this.certThumbprint = this.LoadCertificate(certStream).Thumbprint;
            }
        }

        private X509Certificate2 LoadCertificate(Stream stream)
        {
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return new X509Certificate2(ms.ToArray(), "");
            }
        }

        public bool Verify(string filePath, FileStream openStream)
        {
            if (!AuthenticodeTools.VerifyEmbeddedSignature(filePath, openStream, true))
            {
                logger.Warn("Signature of {0} not valid", filePath);
                return false;
            }

            var cert = new X509Certificate2(filePath);
            if (cert.Thumbprint != this.certThumbprint)
            {
                logger.Warn("Thumbprint of download file {0} {1} does not match expected value of {2}", filePath, cert.Thumbprint, this.certThumbprint);
                return false;
            }

            return true;
        }
    }
}
