using System;
using System.Diagnostics;
using System.IO;

namespace WifiPasswordExtractProxy
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            switch (args.Length)
            {
                case 0:
                    if (Environment.UserName != "SYSTEM")
                    {
                        ExitWithError(5, "ERROR: This application sould be run with SYSTEM user.\n\"%0 help\" to show help");
                        return;
                    }
                    else
                    {
                        Console.WriteLine("BOOTSTRAP: Proxy called from SYSTEM user");
                    }
                    DataExtractor.SystemEntrypointAsync().GetAwaiter().GetResult();
                    break;

                case 1:
                    ArgProcess(args[0]);
                    return;

                case 2:
                    ExportProcess(args[0], args[1]);
                    return;

                default:
                    ExitWithError();
                    return;
            }
        }

        private static void ArgProcess(string arg)
        {
            switch (arg)
            {
                case "regist":
                    if (File.Exists(ExtractProxy.StatusPath)) File.Delete(ExtractProxy.StatusPath);
                    Environment.Exit(RunAsSystem() ? 0 : 2);
                    return;

                case "help":
                    Console.WriteLine(@"Wifi Password Extractor - Proxy Application
Notice: This application not works only this program.
        You should use WifiPasswordExtractor or WifiPasswordExtractorGUI

Args:
regist:
    Query to run this application

help:
    This help

clean:
    Clean all working file
    Notice: This will remove log file too.

export %Export Dir%:
    Export extracted files into directory

:
    Run Extract Process
    Notice: This process must be run with SYSTEM user.
            You can use 'PsExec -s' or 'regist' option");
                    return;

                case "clean":
                    Clean();
                    break;

                default:
                    ExitWithError();
                    break;
            }
        }

        private static void ExportProcess(string a1, string a2)
        {
            if (a1 != "export") ExitWithError();

            if (!Directory.Exists(a2)) ExitWithError(3, "ERROR: export dir not found");

            DataExtractor.UserEntryPoint(Path.GetFullPath(a2)).GetAwaiter().GetResult();

            if (File.Exists(ExtractProxy.StatusPath))
                File.Delete(ExtractProxy.StatusPath);
        }

        private static void ExitWithError(int code = 1, string reason = "ERROR: not valid args")
        {
            Console.WriteLine(reason);
            Environment.Exit(code);
        }

        private static int ExecuteProcess(string executable, string args, bool echo = false)
        {
            var psi = new ProcessStartInfo(executable, args);
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.UseShellExecute = false;
            var p = Process.Start(psi);
            p.WaitForExit();
            if (echo) Console.WriteLine(p.StandardOutput.ReadToEnd());
            if (echo) Console.WriteLine(p.StandardError.ReadToEnd());
            return p.ExitCode;
        }

        private static bool RunAsSystem()
        {
            if (ExtractProxy.ExecutablePath.Length > 252)
            {
                ExitWithError(4, "ERROR: Application found not executable dir. Make path length less than 250");
                return false;
            }

            int p1 = ExecuteProcess("schtasks.exe", $"/create /f /sc Once /tn \"{ExtractProxy.TaskName}\" /tr \"{ExtractProxy.ExecutablePath}\" /st 23:59 /ru \"SYSTEM\" /V1 /Z");
            int p2 = ExecuteProcess("schtasks.exe", $"/run /tn \"{ExtractProxy.TaskName}\"");
            int p3 = ExecuteProcess("schtasks.exe", $"/delete /f /tn \"{ExtractProxy.TaskName}\"");

            //return p1 == 0 && p2 == 0 && p3 == 0;
            return true;
        }

        private static void Clean()
        {
            try
            {
                if (File.Exists(ExtractProxy.LogPath)) File.Delete(ExtractProxy.LogPath);
            }
            catch { }
            try
            {
                if (File.Exists(ExtractProxy.StatusPath)) File.Delete(ExtractProxy.StatusPath);
            }
            catch { }
            try
            {
                if (Directory.Exists(ExtractProxy.ExtractPath)) Directory.Delete(ExtractProxy.ExtractPath, true);
            }
            catch { }
        }
    }
}