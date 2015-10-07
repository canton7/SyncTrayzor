using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SyncTrayzor.Utils
{
    public static class ChecksumFileUtilities
    {
        private static string FormatHash(byte[] hash)
        {
            var hashFormatter = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
            {
                hashFormatter.AppendFormat("{0:x2}", b);
            }

            return hashFormatter.ToString();
        }

        public static void WriteChecksumToFile(HashAlgorithm hashAlgorithm, Stream checksumFile, string filenameToChecksum, Stream fileToChecksum)
        {
            byte[] hash = hashAlgorithm.ComputeHash(fileToChecksum);
            var formattedHash = FormatHash(hash);

            using (var streamWriter = new StreamWriter(checksumFile, Encoding.ASCII, 256, true))
            {
                streamWriter.Write(formattedHash);
                streamWriter.Write("  ");
                streamWriter.Write(filenameToChecksum.Trim());
                streamWriter.WriteLine();
            }
        }

        public static bool ValidateChecksum(HashAlgorithm hashAlgorithm, Stream checksumFile, string filenameToCheck, Stream fileToCheck)
        {
            // Find the checksum...
            string checksum = null;

            using (var checksumFileReader = new StreamReader(checksumFile, Encoding.ASCII, false, 256, true))
            {
                while (checksum == null)
                {
                    var line = checksumFileReader.ReadLine();
                    if (line == null)
                        break;

                    line = line.Trim();
                    var parts = line.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 2)
                        throw new ArgumentException("Invalid format of input file");
                    if (parts[1] == filenameToCheck)
                        checksum = parts[0];
                }
            }

            if (checksum == null)
                throw new ArgumentException($"Could not find checksum for file {filenameToCheck} in checksumFile");

            byte[] hash = hashAlgorithm.ComputeHash(fileToCheck);
            var formattedHash = FormatHash(hash);

            return formattedHash == checksum;
        }
    }
}
