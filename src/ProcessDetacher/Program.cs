using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDetacher
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("At least 1 argument required");
                return 1;
            }

            var startInfo = new ProcessStartInfo()
            {
                FileName = args[0],
                Arguments = String.Join(", ", args.Skip(1).Select(x => String.Format("\"{0}\"", x))),
                CreateNoWindow = true,
            };

            Process.Start(startInfo);

            return 0;
        }
    }
}
