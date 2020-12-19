using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace WifiPasswordDecryptProxy
{
    class Program
    {
        static void Main(string[] args)
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
                    DataDecryptor.DecryptFromQueueFiles().GetAwaiter().GetResult();
                    break;

                case 1:
                    ArgProcess(args[0]);
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
                case "init":
                    if (!Directory.Exists(DecryptProxy.WorkingDirectory))
                        Directory.CreateDirectory(DecryptProxy.WorkingDirectory);
                    var ac_everyfull = new FileSystemAccessRule(new NTAccount("everyone"), FileSystemRights.FullControl, AccessControlType.Allow);
                    var ac_usersfull = new FileSystemAccessRule(new NTAccount("Users"), FileSystemRights.FullControl, AccessControlType.Allow);
                    var acl = Directory.GetAccessControl(DecryptProxy.WorkingDirectory);
                    acl.SetAccessRuleProtection(true, false);
                    acl.AddAccessRule(ac_everyfull);
                    acl.AddAccessRule(ac_usersfull);
                    Directory.SetAccessControl(DecryptProxy.WorkingDirectory, acl);
                    File.WriteAllText(DecryptProxy.DecryptPath, string.Empty);
                    var facl = File.GetAccessControl(DecryptProxy.DecryptPath);
                    facl.AddAccessRule(ac_usersfull);
                    facl.AddAccessRule(ac_everyfull);
                    File.SetAccessControl(DecryptProxy.DecryptPath, facl);
                    break;

                case "regist":
                    if (File.Exists(DecryptProxy.StatusPath)) File.Delete(DecryptProxy.StatusPath);
                    Environment.Exit(RunAsSystem() ? 0 : 2);
                    return;

                case "help":
                    Console.WriteLine($@"Wifi Password Extractor - Proxy Application
Notice: This application not works only this program.
        You should use WifiPasswordExtractor or WifiPasswordExtractorGUI

Args:
init:
    Init query file
    PATH: {DecryptProxy.DecryptPath}

regist:
    Query to run this application

help:
    This help

clean:
    Clean all working file
    Notice: This will remove log file too.

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
            if (DecryptProxy.ExecutablePath.Length > 252)
            {
                ExitWithError(4, "ERROR: Application found not executable dir. Make path length less than 250");
                return false;
            }

            int p1 = ExecuteProcess("schtasks.exe", $"/create /f /sc Once /tn \"{DecryptProxy.TaskName}\" /tr \"{DecryptProxy.ExecutablePath}\" /st 23:59 /ru \"SYSTEM\" /V1 /Z");
            int p2 = ExecuteProcess("schtasks.exe", $"/run /tn \"{DecryptProxy.TaskName}\"");
            int p3 = ExecuteProcess("schtasks.exe", $"/delete /f /tn \"{DecryptProxy.TaskName}\"");

            //return p1 == 0 && p2 == 0 && p3 == 0;
            return true;
        }

        private static void Clean()
        {
            try
            {
                if (File.Exists(DecryptProxy.LogPath)) File.Delete(DecryptProxy.LogPath);
            }
            catch { }
            try
            {
                if (File.Exists(DecryptProxy.StatusPath)) File.Delete(DecryptProxy.StatusPath);
            }
            catch { }
            try
            {
                if (File.Exists(DecryptProxy.DecryptPath)) File.Delete(DecryptProxy.DecryptPath);
            }
            catch { }

            try
            {
                if (Directory.Exists(DecryptProxy.WorkingDirectory)) Directory.Delete(DecryptProxy.WorkingDirectory, true);
            }
            catch { }
        }
    }
}
