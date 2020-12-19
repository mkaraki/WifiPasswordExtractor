using System;
using WifiPasswordExtract;

namespace WifiPasswordExtractor
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var proc = Extractor.ExtractPasswordsAsync();

            Console.WriteLine("System will prompt UAC when scanning.");

            Console.Write("Scanning");
            do
            {
                Console.Write('.');
                System.Threading.Thread.Sleep(500);
            }
            while (!proc.IsCompleted);
            Console.WriteLine();

            if (proc.IsFaulted)
            {
                Console.WriteLine("failed");
#if DEBUG
                Console.ReadLine();
#endif
                return;
            }

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