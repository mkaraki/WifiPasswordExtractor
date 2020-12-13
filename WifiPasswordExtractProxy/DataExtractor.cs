using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WifiPasswordExtractProxy
{
    class DataExtractor
    {
        internal static async Task SystemEntrypointAsync() 
        {
            await Task.Run (() => File.WriteAllText(ExtractProxy.StatusPath, "0", Encoding.ASCII));
        }

        internal static async Task<bool> IsEndAsync()
        { 
            string d = await Task.Run (() => File.ReadAllText(ExtractProxy.StatusPath, Encoding.ASCII));
            return d == "0";
        }

        internal static async Task UserEntryPoint()
        {
            try
            {

            }
            finally
            {
                File.Delete(ExtractProxy.StatusPath);
            }
        }
    }
}
