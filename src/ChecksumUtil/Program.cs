using SyncTrayzor.Utils;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace ChecksumUtil
{
    class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                ShowHelp();
                return;
            }

            try
            {
                var subcommand = args[0];
                switch (subcommand)
                {
                    case "create":
                        Create(args);
                        break;

                    case "verify":
                        Verify(args);
                        break;

                    default:
                        ShowHelp();
                        return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
                Console.Write(e.StackTrace);
                Environment.Exit(1);
            }
        }

        private static void Create(string[] args)
        {
            if (args.Length < 6)
            {
                ShowHelp();
                return;
            }

            var checksumFileName = args[1];
            var algorithmName = args[2];
            var privateKeyName = args[3];
            var passphrase = args[4];
            var inputFileNames = args.Skip(5).ToArray();

            using (var checksumFileTemp = new MemoryStream())
            {
                using (var hashAlgorithm = HashAlgorithm.Create(algorithmName))
                {
                    foreach (var inputFileName in inputFileNames)
                    {
                        using (var inputFile = File.OpenRead(inputFileName))
                        {
                            ChecksumFileUtilities.WriteChecksumToFile(hashAlgorithm, checksumFileTemp, Path.GetFileName(inputFileName), inputFile);
                        }
                    }
                }

                checksumFileTemp.Position = 0;

                using (var checksumFile = File.Create(checksumFileName))
                using (var privateKey = File.OpenRead(privateKeyName))
                {
                    PgpClearsignUtilities.SignFile(checksumFileTemp, checksumFile, privateKey, passphrase.ToCharArray());
                }
            }

            Console.WriteLine("{0} created", checksumFileName);
        }

        private static void Verify(string[] args)
        {
            if (args.Length < 5)
            {
                ShowHelp();
                return;
            }

            var checksumFileName = args[1];
            var algorithmName = args[2];
            var certificateFileName = args[3];
            var inputFileNames = args.Skip(4).ToArray();

            // Signature first, then hash
            using (var checksumFile = File.OpenRead(checksumFileName))
            using (var certificate = File.OpenRead(certificateFileName))
            {
                Stream cleartext;
                var passed = PgpClearsignUtilities.ReadAndVerifyFile(checksumFile, certificate, out cleartext);
                using (cleartext)
                {
                    if (!passed)
                        throw new Exception("Signature verification failed");

                    using (var hashAlgorithm = HashAlgorithm.Create(algorithmName))
                    {
                        foreach (var inputFileName in inputFileNames)
                        {
                            using (var inputFile = File.OpenRead(inputFileName))
                            {
                                var valid = ChecksumFileUtilities.ValidateChecksum(hashAlgorithm, cleartext, Path.GetFileName(inputFileName), inputFile);
                                if (!valid)
                                    throw new Exception($"File {inputFileName} failed checksum");
                            }
                        }
                    }
                }
            }

            Console.WriteLine("All files successfully verified");
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Usage : ChecksumUtil.exe create checksumfile algorithm privatekey passphrase inputfile [inputfile ...]");
            Console.WriteLine("        ChecksumUtil.exe verify checksumfile algorithm certificate inputfile [inputfile ...]");
        }
    }
}
