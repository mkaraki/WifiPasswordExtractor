using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WifiPasswordExtractorAdministratorProxy
{
    public static class AdministratorProxy
    {
        public static readonly string ExecutablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;

        public static readonly string PipeName = "WIFIPASSWORDEXTRACTADMINISTRATOR_SERVICE";
    }
}
