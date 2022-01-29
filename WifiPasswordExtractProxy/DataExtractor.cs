using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WifiPasswordExtractProxy
{
    internal class DataExtractor
    {
        #region Extract

        internal static async Task SystemEntrypointAsync()
        {
            using (var logfs = new FileStream(ExtractProxy.LogPath, FileMode.Create, FileAccess.Write))
            {
                Console.WriteLine("APP: Logfile connected");
                try
                {
                    var rawdatas = ExtractDatasFromRegistryKeys(GetAvailableRegistryKeys());
                    Log(logfs, "Extracted Encrypted raw datas from registry");

                    Log(logfs, $"Cleaning Directory");
                    await CleanExportDirectory(ExtractProxy.ExtractPath, logfs);

                    foreach (var data in rawdatas)
                    {
                        Log(logfs, $"Processing: {data.Key}");
                        byte[] planedata = DecryptRawData(data.Value, logfs);

                        await ExportDataIntoFile(ExtractProxy.ExtractPath, data.Key, planedata, logfs);
                        Log(logfs, $"Processed: {data.Key}");
                    }

                    await Task.Run(() => File.WriteAllText(ExtractProxy.StatusPath, "0", Encoding.ASCII));
                    Log(logfs, "Completed");
                }
                catch (Exception e)
                {
                    Log(logfs, $"Exception Occur: {e.Message}, {e.StackTrace}");
                }
                logfs.Flush();
            }
        }

        private static async void Log(FileStream fs, string log)
        {
            byte[] logbyte = Encoding.UTF8.GetBytes(log + Environment.NewLine);
            await fs.WriteAsync(logbyte, 0, logbyte.Length);
            Console.WriteLine(log);
        }

        private static IEnumerable<RegistryKey> GetAvailableRegistryKeys()
        {
            foreach (var hkuname in Registry.Users.GetSubKeyNames())
            {
                RegistryKey hku;
                try
                {
                    hku = Registry.Users.OpenSubKey(hkuname);
                }
                catch { continue; }

                RegistryKey k1 = null;
                try
                {
                    k1 = hku.OpenSubKey(@"Software\Microsoft\Wlansvc\UserData\Profiles\");
                }
                catch { }

                RegistryKey k2 = null;
                try
                {
                    k2 = hku.OpenSubKey(@"Software\Microsoft\Wlansvc\Profiles\");
                }
                catch { }

                if (k1 != null) yield return k1;
                if (k2 != null) yield return k2;
            }
        }

        private static IEnumerable<KeyValuePair<string, byte[]>> ExtractDatasFromRegistryKeys(IEnumerable<RegistryKey> keys)
        {
            foreach (var key in keys)
                foreach (var subkeyname in key.GetSubKeyNames())
                {
                    var subkey = key.OpenSubKey(subkeyname);
                    byte[] data = (byte[])subkey.GetValue("MSMUserData", null);
                    if (data != null) yield return new KeyValuePair<string, byte[]>(subkeyname, data);
                }
        }

        [Obsolete]
        private static IEnumerable<byte[]> DecryptRawDatas(IEnumerable<byte[]> rawdatas, FileStream log)
        {
            foreach (var rawdata in rawdatas)
            {
                byte[] decryptedrawdata = DecryptRawData(rawdata, log);
                if (decryptedrawdata == null) continue;
                yield return decryptedrawdata;
            }
        }

        private static byte[] DecryptRawData(byte[] rawdata, FileStream log)
        {
            try
            {
                return ProtectedData.Unprotect(rawdata, null, DataProtectionScope.LocalMachine);
            }
            catch (Exception e)
            {
                Log(log, $"Failed to decrypt one of data with exception: {e.Message}");
                return null;
            }
        }

        [Obsolete]
        private static async Task ExportDatasIntoFiles(string dir, byte[][] datas, FileStream log)
        {
            await CleanExportDirectory(dir, log);

            for (int i = 0; i < datas.Length; i++)
            {
                await ExportDataIntoFile(dir, i.ToString(), datas[i], log);
            }
        }

        private static async Task CleanExportDirectory(string dir, FileStream log)
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);

            Directory.CreateDirectory(dir);
            Log(log, $"Directory Ready");
        }

        private static async Task ExportDataIntoFile(string dir, string name, byte[] data, FileStream log)
        {
            string path = Path.Combine(dir, name + ".wpeb");
            await Task.Run(() => File.WriteAllBytes(path, data));
            Log(log, $"{data.Length} bytes exported into \"{path}\"");
        }

        #endregion Extract

        #region User

        internal static async Task UserEntryPoint(string exportdir)
        {
            try
            {
                foreach (var file in GetExtractedFilesPathes())
                {
                    var data = await Task.Run(() => File.ReadAllBytes(file));

                    var dup = TryExtractDomainAndUserFromExtractedData(data);
                    var pass = TryExtractPasswordFromExtractedData(data);

                    if (dup.Item3 == null && pass != null) dup.Item3 = pass;

                    string guid = Path.GetFileNameWithoutExtension(file);

                    await SaveExtractedData(exportdir, guid, dup);
                    Console.WriteLine($"Extracted data of {guid}.");
                }
            }
            finally
            {
                File.Delete(ExtractProxy.StatusPath);
            }
        }

        private static string[] GetExtractedFilesPathes()
        {
            if (!Directory.Exists(ExtractProxy.ExtractPath)) { return null; }
            return Directory.GetFiles(ExtractProxy.ExtractPath, "*.wpeb", SearchOption.TopDirectoryOnly);
        }

        [Obsolete]
        private static IEnumerable<byte[]> GetFilesBytes(IEnumerable<string> pathes)
        {
            foreach (var path in pathes)
                yield return File.ReadAllBytes(path);
        }

        private static readonly byte[] usernamePrefix = { 0x04, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00 };
        private static readonly byte[] alternativeUsernamePrefix = { 0x04, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00 };

        private static (string, string, string) TryExtractDomainAndUserFromExtractedData(byte[] data)
        {
            byte[] rawdomain = null;
            byte[] rawuname = null;
            byte[] rawpassword = null;

            string domain = null;
            string user = null;
            string password = null;

            var unamefieldsearch = FindBytesSequences(data, usernamePrefix);
            if (unamefieldsearch.Any())
            {
                int unamefield = unamefieldsearch.First() + usernamePrefix.Length;
                int unamefieldend = FindByteSequence(data, 0x00, unamefield);

                if (unamefieldend == -1)
                {
                    // If end of username field not found.
                    Console.WriteLine("Failed");
                    goto Decode;
                }

                rawuname = CopyBytesRange(data, unamefield, unamefieldend);

                int domfield = FindNotByteSequence(data, 0x00, unamefieldend + 1);
                if (domfield == -1 || data[domfield] == 0xE6) goto Decode;

                // If domain field found.
                int domfieldend = FindByteSequence(data, 0x00, domfield);
                if (domfieldend == -1) goto Decode;

                // If end of domain field found.
                rawdomain = CopyBytesRange(data, domfield, domfieldend);
            }
            else
            {
                var unamefieldsearchA = FindBytesSequences(data, alternativeUsernamePrefix);
                if (unamefieldsearchA.Any())
                {
                    int unamefield = unamefieldsearch.First() + usernamePrefix.Length;
                    int unamefieldend = FindByteSequence(data, 0x00, unamefield);

                    if (unamefieldend == -1)
                    {
                        // If end of username field not found.
                        Console.WriteLine("Failed");
                        goto Decode;
                    }

                    rawuname = CopyBytesRange(data, unamefield, unamefieldend);

                    int passfield = FindNotByteSequence(data, 0x00, unamefieldend);
                    if (passfield == -1) goto Decode;

                    // If passfield found
                    int passfieldend = FindByteSequence(data, 0x00, passfield);
                    if (passfieldend == -1) goto Decode;

                    // If end of pass field found.
                    rawpassword = CopyBytesRange(data, passfield, passfieldend);

                    int domfield = FindNotByteSequence(data, 0x00, unamefieldend + 1);
                    if (domfield == -1 || data[domfield] == 0xE6) goto Decode;

                    // If domain field found.
                    int domfieldend = FindByteSequence(data, 0x00, domfield);
                    if (domfieldend == -1) goto Decode;

                    // If end of domain field found.
                    rawdomain = CopyBytesRange(data, domfield, domfieldend);
                }
            }

        Decode:

            if (rawuname != null) user = Encoding.ASCII.GetString(rawuname, 0, rawuname.Length).Trim('\0');
            if (rawdomain != null) domain = Encoding.ASCII.GetString(rawdomain, 0, rawdomain.Length).Trim('\0');
            if (rawpassword != null) password = Encoding.ASCII.GetString(rawpassword, 0, rawpassword.Length).Trim('\0');

            return (domain, user, password);
        }

        private static readonly byte[] passwordPrefix = { 0x01, 0x00, 0x00, 0x00, 0xD0, 0x8C, 0x9D, 0xDF, 0x01 };

        private static string TryExtractPasswordFromExtractedData(byte[] data)
        {
            byte[] rawpassword = null;

            string password = null;

            var passfieldsearch = FindBytesSequences(data, passwordPrefix);
            if (!passfieldsearch.Any()) goto Decode;

            byte[] rawpasswordenc = data.Skip(passfieldsearch.First()).ToArray();

            try
            {
                byte[] rawpasswordv = ProtectedData.Unprotect(rawpasswordenc, null, DataProtectionScope.LocalMachine);
                int endofpass = FindByteSequence(rawpasswordv, 0x00, 0);
                if (endofpass == -1) rawpassword = rawpasswordv;
                else
                {
                    rawpassword = CopyBytesRange(rawpasswordv, 0, endofpass);
                }
            }
            catch
            {
                Console.WriteLine("Failed to decrypt password");
                goto Decode;
            }

        Decode:
            if (rawpassword != null) password = Encoding.ASCII.GetString(rawpassword, 0, rawpassword.Length).Trim('\0');

            return password;
        }

        private static int FindNotByteSequence(byte[] src, byte safe, int offset = 0)
        {
            for (int i = offset; i < src.Length; i++)
            {
                if (src[i] != safe)
                    return i;
            }

            return -1;
        }

        private static int FindByteSequence(byte[] src, byte target, int offset = 0)
        {
            for (int i = offset; i < src.Length; i++)
            {
                if (src[i] == target)
                    return i;
            }

            return -1;
        }

        private static IEnumerable<int> FindBytesSequences(byte[] src, byte[] target, int offset = 0)
        {
            // ref: https://teratail.com/questions/20408#reply-32075
            return src
                .Select((v, i) => new { index = i, value = src.Skip(i).Take(target.Length) })
                .Where(v => v.value.SequenceEqual(target))
                .Select(v => v.index)
                .Where(v => v >= 0);
        }

        private static byte[] CopyBytesRange(byte[] array, int startindex, int endindex)
        {
            return array.Skip(startindex).Take(endindex - startindex + 1).ToArray();
        }

        private static async Task SaveExtractedData(string dir, string name, (string, string, string) dup)
        {
            string path = Path.Combine(dir, name + ".wpei");
            string data = $"{dup.Item1}\n{dup.Item2}\n{dup.Item3}";
            await Task.Run(() => {
                File.WriteAllText(path, data, Encoding.UTF8);

                // TODO: Use C# builtin FileSecurity
                System.Diagnostics.Process.Start("icacls", $"\"{path}\" /grant Everyone:F").WaitForExit();
            });
        }

        #endregion User
    }
}