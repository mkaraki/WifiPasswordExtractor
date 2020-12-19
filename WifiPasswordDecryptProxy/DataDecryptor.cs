using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace WifiPasswordDecryptProxy
{
    static class DataDecryptor
    {
        internal static async Task DecryptFromQueueFiles()
        {
            string[] b64queuefile = await Task.Run(() => File.ReadAllLines(DecryptProxy.DecryptPath, Encoding.ASCII));
            List<string> decrypteddata = new List<string>();

            foreach (var b64es in b64queuefile)
            {
                byte[] eb = Convert.FromBase64String(b64es);
                string b64ds = string.Empty;
                try
                {
                    byte[] db = await Task.Run (() => ProtectedData.Unprotect(eb, null, DataProtectionScope.LocalMachine));
                    b64ds = Convert.ToBase64String(db);
                }
                catch
                {
                    continue;
                }
                decrypteddata.Add(b64es + ',' + b64ds);
            }

            await Task.Run (() => File.WriteAllLines(DecryptProxy.DecryptPath, decrypteddata.ToArray(), Encoding.ASCII));
            
            await Task.Run (() => File.WriteAllText(DecryptProxy.StatusPath, string.Empty));
        }
    }
}
