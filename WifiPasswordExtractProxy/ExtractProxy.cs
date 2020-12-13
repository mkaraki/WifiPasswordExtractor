namespace WifiPasswordExtractProxy
{
    public static class ExtractProxy
    {
        public static readonly string ExecutablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;

        internal static readonly string TaskName = "WIFIPASSWORDEXTRACT_SERVICE";

        public static readonly string StatusPath = $@"C:\{TaskName}_STATUS.wpes";

        internal static readonly string LogPath = $@"C:\{TaskName}_LOG.log";

        public static readonly string ExtractPath = $@"C:\{TaskName}_EXTRACTED";
    }
}