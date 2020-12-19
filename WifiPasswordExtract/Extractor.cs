using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace WifiPasswordExtract
{
    public class Extractor
    {
        private const string WIFI_PROFILES_BASEDIR = @"C:\ProgramData\Microsoft\Wlansvc\Profiles\Interfaces";

        private static XmlSerializer wlanProfileSerializer = new XmlSerializer(typeof(WLANProfileXml.WLANProfile));

        public static bool UseLegacyNetshBasedPasswordExtract = true;

        public static async Task<IEnumerable<WifiCredential>> ExtractPasswordsAsync()
        {
            RunAdministratorProxyWithServer();

            List<WifiCredential> toret = new List<WifiCredential>();

            try
            {
                toret.AddRange(await ProcessOneXPassword());
            }
            catch { }

            try
            {
                toret = (await ProcessNotEnterprisePasswords(toret)).ToList();
            }
            catch { }

            RunAdministrativeProcessWithProxy(null);

            return toret;
        }

        #region OneX

        private static async Task<IEnumerable<WifiCredential>> ProcessOneXPassword()
        {
            RunAdministrativeProcessWithProxy(WifiPasswordExtractProxy.ExtractProxy.ExecutablePath, "regist");

            do
            {
                await Task.Delay(500);
            }
            while (!File.Exists(WifiPasswordExtractProxy.ExtractProxy.StatusPath));

            return await ExportOneXPassword();
        }

        private static async Task<IEnumerable<WifiCredential>> ExportOneXPassword()
        {
            var rnd = new Random();
            var tempdir = Path.Combine(Path.GetTempPath(), $"WIFIPASSWORDEXTRACT_{rnd.Next(0, int.MaxValue)}");

            Debug.WriteLine(tempdir);

            if (Directory.Exists(tempdir))
                return await ExportOneXPassword();

            try
            {
                Directory.CreateDirectory(tempdir);
            }
            catch
            {
                throw new Exception("Permission Denied");
            }

            RunAdministrativeProcessWithProxy(WifiPasswordExtractProxy.ExtractProxy.ExecutablePath, $"export \"{tempdir}\"");
            do
            {
                await Task.Delay(500);
            }
            while (File.Exists(WifiPasswordExtractProxy.ExtractProxy.StatusPath));

            return await GetOneXDatasInExportedDirectory(tempdir);
        }

        private static async Task<IEnumerable<WifiCredential>> GetOneXDatasInExportedDirectory(string dir)
        {
            List<WifiCredential> toret = new List<WifiCredential>();
            foreach (var file in Directory.GetFiles(dir, "*.wpei", SearchOption.TopDirectoryOnly))
            {
                string[] data = await Task.Run(() => File.ReadAllLines(file, Encoding.UTF8));
                if (data.Length != 3) continue;
                var c = new WifiCredential(string.Empty, data[0], data[1], data[2]);
                c.GUID = Path.GetFileNameWithoutExtension(file);
                toret.Add(c);
            }

            RunAdministrativeProcessWithProxy(WifiPasswordExtractProxy.ExtractProxy.ExecutablePath, $"clean");

            return toret;
        }

        #endregion OneX

        #region Personal

        private static async Task<IEnumerable<WifiCredential>> ProcessNotEnterprisePasswords(List<WifiCredential> host)
        {
            List<WifiCredential> toret = host;

            List<WifiCredential> waitfordecrypt = new List<WifiCredential>();

            foreach (var profile in await GetWlanProfilesFromDirectoryAsync())
            {
                if (profile.MSM.security.authEncryption.useOneX)
                {
                    var guidcheck = toret.Where(v => v.GUID == profile.guid);
                    if (guidcheck.Any())
                    {
                        guidcheck.First().SSID = profile.SSIDConfig.SSID.name;
                    }
                    continue;
                }

                if (profile.MSM.security.sharedKey != null && !profile.MSM.security.sharedKey.@protected)
                {
                    var c = new WifiCredential(profile.SSIDConfig.SSID.name, profile.MSM.security.sharedKey.keyMaterial);
                    c.GUID = profile.guid;
                    toret.Add(c);
                    continue;
                }
                else if (profile.MSM.security.authEncryption.authentication == "open")
                {
                    var c = new WifiCredential(profile.SSIDConfig.SSID.name);
                    c.GUID = profile.guid;
                    toret.Add(c);
                    continue;
                }

                if (profile.MSM.security.sharedKey != null && profile.MSM.security.sharedKey.@protected)
                {
                    if (UseLegacyNetshBasedPasswordExtract)
                    {
                        var dprofile = await GetWlanProfilesFromNetshWithProfileNameAsync(profile.name);
                        if (dprofile.Length > 0 && !dprofile[0].MSM.security.sharedKey.@protected)
                        {
                            var c = new WifiCredential(profile.SSIDConfig.SSID.name, dprofile[0].MSM.security.sharedKey.keyMaterial);
                            c.GUID = profile.guid;
                            toret.Add(c);
                            continue;
                        }
                        else
                        {
                            var b64km = KeyMaterialToBase64(profile.MSM.security.sharedKey.keyMaterial);
                            var c = new WifiCredential(profile.SSIDConfig.SSID.name, b64km);
                            c.GUID = profile.guid;
                            waitfordecrypt.Add(c);
                        }
                    }
                }
            }

            if (!UseLegacyNetshBasedPasswordExtract)
            { 
                var keys = await DecryptPasswordsBySYSTEMAccount(waitfordecrypt.Select(v => v.Password).ToArray());
                List<WifiCredential> newcred = new List<WifiCredential>();
                foreach (var key in keys)
                {
                    var searcher = waitfordecrypt.Where(v => v.Password == key.Key);
                    if (!searcher.Any()) continue;
                    var cred = searcher.First();
                    cred.Password = key.Value;
                    newcred.Add(cred);
                }
                toret.AddRange(newcred);
            }

            return toret;
        }

        private static string KeyMaterialToBase64(string keyMaterial)
        {
            byte[] binkey = new byte[keyMaterial.Length / 2];
            for (int i = 0; i < keyMaterial.Length; i += 2)
            {
                binkey[i / 2] = Convert.ToByte(keyMaterial.Substring(i, 2), 16);
            }
            return Convert.ToBase64String(binkey);
        }

        private static async Task<Dictionary<string, string>> DecryptPasswordsBySYSTEMAccount(string[] b64KeyMeterials)
        {
            RunAdministrativeProcessWithProxy(WifiPasswordDecryptProxy.DecryptProxy.ExecutablePath, "init");
            do
            {
                await Task.Delay(500);
            }
            while (!File.Exists(WifiPasswordDecryptProxy.DecryptProxy.DecryptPath));

            await Task.Run(() => File.WriteAllLines(WifiPasswordDecryptProxy.DecryptProxy.DecryptPath, b64KeyMeterials, Encoding.ASCII));
            RunAdministrativeProcessWithProxy(WifiPasswordDecryptProxy.DecryptProxy.ExecutablePath, "regist");
            do
            {
                await Task.Delay(500);
            }
            while (!File.Exists(WifiPasswordDecryptProxy.DecryptProxy.StatusPath));

            string[] dis = await Task.Run(() => File.ReadAllLines(WifiPasswordDecryptProxy.DecryptProxy.DecryptPath, Encoding.ASCII));
            Dictionary<string, string> toret = new Dictionary<string, string>();
            foreach (var di in dis)
            {
                string[] dii = di.Split(',');
                if (dii.Length != 2) continue;
                toret.Add(dii[0], dii[1]);
            }

            RunAdministrativeProcessWithProxy(WifiPasswordDecryptProxy.DecryptProxy.ExecutablePath, "clean");

            return toret;
        }

        private static async Task<WLANProfileXml.WLANProfile[]> GetWlanProfilesFromDirectoryAsync(string directory = WIFI_PROFILES_BASEDIR)
        {
            var parsedprofiles = new List<WLANProfileXml.WLANProfile>();

            string[] profiles = Directory.GetFiles(directory, "*.xml", SearchOption.AllDirectories);
            foreach (string profilepath in profiles)
            {
                WLANProfileXml.WLANProfile profile = null;
                using (var sr = new StreamReader(profilepath))
                {
                    profile = await Task.Run(() => (WLANProfileXml.WLANProfile)wlanProfileSerializer.Deserialize(sr));
                }

                if (profile == null) continue;
                if (profile.name == null) continue;

                profile.guid = Path.GetFileNameWithoutExtension(profilepath);

                parsedprofiles.Add(profile);
            }

            return parsedprofiles.ToArray();
        }

        private static async Task<WLANProfileXml.WLANProfile[]> GetWlanProfilesFromNetshWithProfileNameAsync(string profileName)
        {
            var toret = new WLANProfileXml.WLANProfile[0];

            var rnd = new Random();
            var tempdir = Path.Combine(Path.GetTempPath(), $"WIFIPASSWORDEXTRACT_{rnd.Next(0, int.MaxValue)}");

            Debug.WriteLine(tempdir);

            if (Directory.Exists(tempdir))
                return await GetWlanProfilesFromNetshWithProfileNameAsync(profileName);

            try
            {
                Directory.CreateDirectory(tempdir);
            }
            catch
            {
                throw new Exception("Permission Denied");
            }

            try
            {
                bool res = await ExportProfileUsingNetshWithClearPasswordAsync(profileName, tempdir);
                if (res)
                {
                    toret = await GetWlanProfilesFromDirectoryAsync(tempdir);
                }
            }
            finally
            {
                try
                {
                    Directory.Delete(tempdir, true);
                }
                catch
                {
                    Debug.WriteLine("Failed to remove directory {0}", tempdir);
                }
            }

            return toret;
        }

        private static async Task<bool> ExportProfileUsingNetshWithClearPasswordAsync(string profileName, string dir)
        {
            var psi = new ProcessStartInfo("netsh.exe", $"wlan export profile name=\"{profileName}\" key=clear folder={dir}");
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            var p = Process.Start(psi);
            await Task.Run(() => p.WaitForExit());
            return p.ExitCode == 0;
        }

        #endregion Personal

        private static Queue<(string, string)> AdministrativeExecuteQueries = new Queue<(string, string)>();

        private static void RunAdministratorProxyWithServer()
        {
            var psi = new ProcessStartInfo(WifiPasswordExtractorAdministratorProxy.AdministratorProxy.ExecutablePath);
            psi.UseShellExecute = true;
            psi.CreateNoWindow = true;
            var p = Process.Start(psi);

            Task.Run(async () => { 
                try
                {
                    using (var stream = new NamedPipeServerStream(WifiPasswordExtractorAdministratorProxy.AdministratorProxy.PipeName))
                    {
                        await Task.Run(() => stream.WaitForConnection());

                        using (var writer = new StreamWriter(stream))
                        {
                            writer.AutoFlush = true;
                            while (true)
                            {
                                if (AdministrativeExecuteQueries.Count < 1) continue;
                                var queue = AdministrativeExecuteQueries.Dequeue();
                                if (queue.Item1 == null)
                                {
                                    p.Kill();
                                    break;
                                }
                                await writer.WriteLineAsync($"exec\0{queue.Item1}\0{queue.Item2}");
                            }
                        }
                    }
                }
                catch (IOException)
                { }
                finally
                {
                    if (!p.HasExited)
                        p.Kill();
                }
            });
        }

        private static void RunAdministrativeProcessWithProxy(string bin, string arg = "")
        {
            AdministrativeExecuteQueries.Enqueue((bin, arg));   
        }
    }
}