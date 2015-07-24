using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using System;
using System.IO;

namespace SyncTrayzor.Utils
{
    public static class PgpClearsignUtilities
    {
        private static PgpSecretKey ReadSecretKey(Stream inputStream)
        {
            var decodedInputStream = PgpUtilities.GetDecoderStream(inputStream);
            var secretKeyRingBundle = new PgpSecretKeyRingBundle(decodedInputStream);

            // we just loop through the collection till we find a key suitable for encryption, in the real
            // world you would probably want to be a bit smarter about this.
            foreach (PgpSecretKeyRing keyRing in secretKeyRingBundle.GetKeyRings())
            {
                foreach (PgpSecretKey key in keyRing.GetSecretKeys())
                {
                    if (key.IsSigningKey)
                        return key;
                }
            }

            throw new ArgumentException("Can't find signing key in key ring.");
        }

        public static void SignFile(Stream input, Stream outputStream, Stream keyIn, char[] pass)
        {
            var secretKey = ReadSecretKey(keyIn);
            var privateKey = secretKey.ExtractPrivateKey(pass);

            var signatureGenerator = new PgpSignatureGenerator(secretKey.PublicKey.Algorithm, HashAlgorithmTag.Sha1);
            var subpacketGenerator = new PgpSignatureSubpacketGenerator();

            signatureGenerator.InitSign(PgpSignature.CanonicalTextDocument, privateKey);
            foreach (string userId in secretKey.PublicKey.GetUserIds())
            {
                var signatureSubpacketGenerator = new PgpSignatureSubpacketGenerator();
                signatureSubpacketGenerator.SetSignerUserId(isCritical: false, userId: userId);
                signatureGenerator.SetHashedSubpackets(signatureSubpacketGenerator.Generate());
                // Just the first one!
                break;
            }

            // Closing armouredOutputStream does not close the underlying stream
            var armouredOutputStream = new ArmoredOutputStream(outputStream);
            using (var bcpgOutputStream = new BcpgOutputStream(armouredOutputStream))
            {
                armouredOutputStream.BeginClearText(HashAlgorithmTag.Sha1);

                int chr;
                while ((chr = input.ReadByte()) > 0)
                {
                    signatureGenerator.Update((byte)chr);
                    bcpgOutputStream.Write((byte)chr);
                }

                // For some reason we need to add a trailing newline
                bcpgOutputStream.Write((byte)'\n'); 

                armouredOutputStream.EndClearText();

                signatureGenerator.Generate().Encode(bcpgOutputStream);
            }
        }

        public static bool ReadAndVerifyFile(Stream inputStream, Stream keyIn, out Stream cleartextOut)
        {
            // Count any exception as BouncyCastle failing to parse something, because of corruption maybe?
            try
            {

                // Disposing this will close the underlying stream, which we don't want to do
                var armouredInputStream = new ArmoredInputStream(inputStream);

                // This stream is returned, so is not disposed
                var cleartextStream = new MemoryStream();

                int chr;

                while ((chr = armouredInputStream.ReadByte()) >= 0 && armouredInputStream.IsClearText())
                {
                    cleartextStream.WriteByte((byte)chr);
                }

                // Strip the trailing newline if set...
                cleartextStream.Position = Math.Max(0, cleartextStream.Position - 2);
                int count = 0;
                if (cleartextStream.ReadByte() == '\r')
                    count++;
                if (cleartextStream.ReadByte() == '\n')
                    count++;
                cleartextStream.SetLength(cleartextStream.Length - count);

                cleartextStream.Position = 0;

                // This will either return inputStream, or a new ArmouredStream(inputStream)
                // Either way, disposing it will close the underlying stream, which we don't want to do
                var decoderStream = PgpUtilities.GetDecoderStream(inputStream);

                var pgpObjectFactory = new PgpObjectFactory(decoderStream);

                var signatureList = (PgpSignatureList)pgpObjectFactory.NextPgpObject();
                var signature = signatureList[0];

                var publicKeyRing = new PgpPublicKeyRingBundle(PgpUtilities.GetDecoderStream(keyIn));
                var publicKey = publicKeyRing.GetPublicKey(signature.KeyId);

                signature.InitVerify(publicKey);

                while ((chr = cleartextStream.ReadByte()) > 0)
                {
                    signature.Update((byte)chr);
                }
                cleartextStream.Position = 0;

                cleartextOut = cleartextStream;
                return signature.Verify();
            }
            catch
            {
                cleartextOut = null;
                return false;
            }
        }
    }
}
