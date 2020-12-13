using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        public static async Task<IEnumerable<WifiCredential>> ExtractPasswordsAsync()
        {
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

            return toret;
        }

        #region OneX

        private static async Task<IEnumerable<WifiCredential>> ProcessOneXPassword()
        {
            var proxy = WifiPasswordExtractProxy.ExtractProxy.ExecutablePath;

            var p = Process.Start(proxy, "regist");
            await Task.Run(() => p.WaitForExit());
            if (p.ExitCode != 0) return null;

            do
            {
                await Task.Delay(500);
            }
            while (!File.Exists(WifiPasswordExtractProxy.ExtractProxy.StatusPath));

            return await ExportOneXPassword(proxy);
        }

        private static async Task<IEnumerable<WifiCredential>> ExportOneXPassword(string proxy)
        {
            var rnd = new Random();
            var tempdir = Path.Combine(Path.GetTempPath(), $"WIFIPASSWORDEXTRACT_{rnd.Next(0, int.MaxValue)}");

            Debug.WriteLine(tempdir);

            if (Directory.Exists(tempdir))
                return await ExportOneXPassword(proxy);

            try
            {
                Directory.CreateDirectory(tempdir);
            }
            catch
            {
                throw new Exception("Permission Denied");
            }

            var p = Process.Start(proxy, $"export \"{tempdir}\"");
            await Task.Run(() => p.WaitForExit());
            if (p.ExitCode != 0) return null;

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
            return toret;
        }

        #endregion OneX

        #region Personal

        private static async Task<IEnumerable<WifiCredential>> ProcessNotEnterprisePasswords(List<WifiCredential> host)
        {
            List<WifiCredential> toret = host;

            foreach (var profile in await GetWlanProfilesFromDirectoryAsync())
            {
                if (profile.MSM.security.authEncryption.useOneX)
                {
                    var guidcheck = toret.Where(v=> v.GUID == profile.guid);
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
                    var dprofile = await GetWlanProfilesFromNetshWithProfileNameAsync(profile.name);
                    if (dprofile.Length > 0 && !dprofile[0].MSM.security.sharedKey.@protected)
                    {
                        var c = new WifiCredential(profile.SSIDConfig.SSID.name, dprofile[0].MSM.security.sharedKey.keyMaterial);
                        c.GUID = profile.guid;
                        toret.Add(c);
                        continue;
                    }
                }

            }

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
            var p = Process.Start(psi);
            await Task.Run(() => p.WaitForExit());
            return p.ExitCode == 0;
        }

        #endregion Personal
    }
}