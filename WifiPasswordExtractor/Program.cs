using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WifiPasswordExtract;

namespace WifiPasswordExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            Extractor.ExtractPasswordsAsync().GetAwaiter().GetResult();
        }
    }
}
