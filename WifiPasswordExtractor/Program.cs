using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WifiPasswordExtract;

namespace WifiPasswordExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            var proc = Extractor.ExtractPasswordsAsync();

            Console.WriteLine("System may prompt UAC 2 times in scan.");

            Console.Write("Scanning");
            do
            {
                Console.Write('.');
                System.Threading.Thread.Sleep(500);
            }
            while (!proc.IsCompleted);
            Console.WriteLine();

            Console.WriteLine("========== RESULT ==========");
            foreach (var cred in proc.Result)
            {
                Console.WriteLine(cred.ToString());
            }

#if DEBUG
            Console.ReadLine();
#endif
        }
    }
}
