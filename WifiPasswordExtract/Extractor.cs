using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace WifiPasswordExtract
{
    public class Extractor
    {
        private const string WIFI_PROFILES_BASEDIR = @"C:\ProgramData\Microsoft\Wlansvc\Profiles\Interfaces";

        private static XmlSerializer wlanProfileSerializer = new XmlSerializer(typeof(WLANProfileXml.WLANProfile));

        public static async Task ExtractPasswordsAsync()
        {
            await ProcessNotEnterprisePasswords();
            // await ProcessOneXPassword();
        }

        private static async Task ProcessOneXPassword()
        {
            var proxy = WifiPasswordExtractProxy.ExtractProxy.ExecutablePath;
            Console.WriteLine("Proxy: {0}", proxy);

            var p = Process.Start(proxy, "regist");
            await Task.Run (() => p.WaitForExit());
            if (p.ExitCode != 0) return;


        }

        private static async Task ProcessNotEnterprisePasswords()
        {
            foreach (var profile in await GetWlanProfilesFromDirectoryAsync())
            {
                if (profile.MSM.security.authEncryption.useOneX) continue;

                if (profile.MSM.security.authEncryption.authentication != "open" && profile.MSM.security.sharedKey != null)
                {
                    Console.Write("{0}: {1}",
                        profile.SSIDConfig.SSID.name,
                        profile.MSM.security.sharedKey.@protected ? string.Empty : profile.MSM.security.sharedKey.keyMaterial);
                }
                else
                {
                    Console.Write(profile.SSIDConfig.SSID.name);
                }

                if (profile.MSM.security.sharedKey != null && profile.MSM.security.sharedKey.@protected)
                {
                    var dprofile = await GetWlanProfilesFromNetshWithProfileNameAsync(profile.name);
                    if (dprofile.Length < 1)
                        Console.Write("FAILED");
                    else
                        Console.Write(dprofile[0].MSM.security.sharedKey.keyMaterial);
                }

                Console.WriteLine();
            }
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
                    profile = await Task.Run (() => (WLANProfileXml.WLANProfile)wlanProfileSerializer.Deserialize(sr));
                }

                if (profile == null) continue;
                if (profile.name == null) continue;

                parsedprofiles.Add(profile);
            }

            return parsedprofiles.ToArray();
        }

        private static async Task<WLANProfileXml.WLANProfile[]> GetWlanProfilesFromNetshWithProfileNameAsync(string profileName)
        {
            var toret = new WLANProfileXml.WLANProfile[0];

            var rnd = new Random();
            var tempdir = Path.Combine(Path.GetTempPath(), $"WIFIPASSWORDEXTRACT_{rnd.Next(0,int.MaxValue)}");

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
    }
}