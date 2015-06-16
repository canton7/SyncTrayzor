using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChecksumCreator
{
    // From the SignedFileProcessor sample, and http://stackoverflow.com/a/18796555/1086121
    class Program
    {
        static void Main(string[] args)
        {
            //using (var key = File.OpenRead("secret_key.gpg"))
            //using (var outputStream = File.OpenWrite("testfile2.txt.asc"))
            //{
            //    SignFile(File.ReadAllBytes("testfile2.txt"), outputStream, key, "password".ToCharArray());
            //}

            using (var key = File.OpenRead("certificate.asc"))
            using (var input = File.OpenRead("testfile2.txt.asc"))
            {
                VerifyFile(input, key);
            }
        }

        private static PgpSecretKey ReadSecretKey(Stream inputStream)
        {
            var decodedInputStream = PgpUtilities.GetDecoderStream(inputStream);
            PgpSecretKeyRingBundle pgpSec = new PgpSecretKeyRingBundle(decodedInputStream);

            // we just loop through the collection till we find a key suitable for encryption, in the real
            // world you would probably want to be a bit smarter about this.
            foreach (PgpSecretKeyRing keyRing in pgpSec.GetKeyRings())
            {
                foreach (PgpSecretKey key in keyRing.GetSecretKeys())
                {
                    if (key.IsSigningKey)
                        return key;
                }
            }

            throw new ArgumentException("Can't find signing key in key ring.");
        }

        private static void SignFile(byte[] input, Stream outputStream, Stream keyIn, char[] pass)
        {
            var secretKey = ReadSecretKey(keyIn);
            var privateKey = secretKey.ExtractPrivateKey(pass);

            PgpSignatureGenerator signatureGenerator = new PgpSignatureGenerator(secretKey.PublicKey.Algorithm, HashAlgorithmTag.Sha1);
            PgpSignatureSubpacketGenerator subpacketGenerator = new PgpSignatureSubpacketGenerator();

            signatureGenerator.InitSign(PgpSignature.CanonicalTextDocument, privateKey);
            foreach (string userId in secretKey.PublicKey.GetUserIds())
            {
                PgpSignatureSubpacketGenerator spGen = new PgpSignatureSubpacketGenerator();
                spGen.SetSignerUserId(false, userId);
                signatureGenerator.SetHashedSubpackets(spGen.Generate());
                // Just the first one!
                break;
            }

            var armouredOutputStream = new ArmoredOutputStream(outputStream);

            BcpgOutputStream bcpgOutputStream = new BcpgOutputStream(armouredOutputStream);

            armouredOutputStream.BeginClearText(HashAlgorithmTag.Sha1);

            signatureGenerator.Update(input);
            bcpgOutputStream.Write(input);
            bcpgOutputStream.Write((byte)'\n'); // For some reason this needs adding

            armouredOutputStream.EndClearText();

            signatureGenerator.Generate().Encode(bcpgOutputStream);
        }

        private static byte[] VerifyFile(Stream inputStream, Stream keyIn)
        {
            byte[] cleartext;
            var armouredInputStream = new ArmoredInputStream(inputStream);

            using (var cleartextStream = new MemoryStream())
            {
                int ch = 0;
                while ((ch = armouredInputStream.ReadByte()) >= 0 && armouredInputStream.IsClearText())
                {
                    cleartextStream.WriteByte((byte)ch);
                }

                // Strip the trailing newline if set...
                cleartextStream.Seek(-1, SeekOrigin.End);
                if (cleartextStream.ReadByte() == '\n')
                    cleartextStream.SetLength(cleartextStream.Length - 1);

                cleartext = cleartextStream.ToArray();
            }

            using (var decoderStream = PgpUtilities.GetDecoderStream(inputStream))
            {
                PgpObjectFactory pgpObjectFactory = new PgpObjectFactory(decoderStream);

                PgpSignatureList signatureList = (PgpSignatureList)pgpObjectFactory.NextPgpObject();
                PgpSignature signature = signatureList[0];

                PgpPublicKeyRingBundle publicKeyRing = new PgpPublicKeyRingBundle(PgpUtilities.GetDecoderStream(keyIn));
                PgpPublicKey publicKey = publicKeyRing.GetPublicKey(signature.KeyId);

                signature.InitVerify(publicKey);
                signature.Update(cleartext);

                if (signature.Verify())
                {
                    Console.WriteLine("signature verified.");
                }
                else
                {
                    Console.WriteLine("signature verification failed.");
                }
            }

            return cleartext;
        }
    }
}
