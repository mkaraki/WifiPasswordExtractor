using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace WifiPasswordExtractorAdministratorProxy
{
    class Program
    {
        // Ref: https://stackoverflow.com/a/3571628
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;


        static readonly string[] WhitelistedBinary = new string[] {
            WifiPasswordDecryptProxy.DecryptProxy.ExecutablePath,
            WifiPasswordExtractProxy.ExtractProxy.ExecutablePath,
            };

        static void Main(string[] args)
        {
#if !DEBUG
            ShowWindow(GetConsoleWindow(), SW_HIDE);
#endif

            Console.WriteLine(@"Wi-Fi Password Extractor Administrator Proxy
This Program will able to run few Administrative Processes.

DON'T CLOSE DURING SCANNING.");


                using (var stream = new NamedPipeClientStream(AdministratorProxy.PipeName))
                {
                    Task.Run(() => stream.Connect()).Wait();

                    using (var reader = new StreamReader(stream))
                    {
                        while (stream.IsConnected)
                        {
                            string str = reader.ReadLine();
                            var cmd = str.Split('\0');
                            if (cmd.Length != 3) continue;
                            Console.WriteLine("Execution Queried: {0} {1}", cmd[1], cmd[2]);
                            switch (cmd[0])
                            {
                                case "exec":
                                    Execute(cmd[1], cmd[2]);
                                    break;

                                case "execwait":
                                    Task.Run(() => ExecuteAndWaitAsync(cmd[1], cmd[2]));
                                    break;

                                default:
                                    continue;
                            }
                        }
                    }
                }

            while (true) 
            {
                if (Console.ReadLine() == "exit")
                    break;
            }
        }

        static async Task<Process> ExecuteAndWaitAsync(string bin, string arg)
        {
            var p = Execute(bin, arg);
            if (p == null) return null;
            await Task.Run(() => p.WaitForExit());
            return p;
        }

        static Process Execute(string bin, string arg)
        {
            if (!WhitelistedBinary.Contains(bin)) return null;

            var psi = new ProcessStartInfo(bin, arg);
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            var p = Process.Start(psi);
            return p;
        }
    }
}
