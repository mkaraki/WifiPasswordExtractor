namespace WifiPasswordExtract
{
    public class WifiCredential
    {
        /// <summary>
        /// Regist Open Wi-Fi
        /// </summary>
        /// <param name="SSID"></param>
        public WifiCredential(string SSID)
        {
            this.SSID = SSID;
            this.Open = true;
            this.OneX = false;
        }

        /// <summary>
        /// Regist Personal Secured Wi-Fi
        /// </summary>
        /// <param name="SSID"></param>
        /// <param name="Passprase"></param>
        public WifiCredential(string SSID, string Passprase) : this(SSID)
        {
            Password = Passprase;
            this.Open = false;
        }

        /// <summary>
        /// Regist Enterprise Secured Wi-Fi
        /// </summary>
        /// <param name="SSID"></param>
        /// <param name="Domain"></param>
        /// <param name="Username"></param>
        /// <param name="Password"></param>
        public WifiCredential(string SSID, string Domain, string Username, string Password) : this(SSID, Password)
        {
            this.Domain = Domain;
            this.Username = Username;
            this.OneX = true;
        }

        /// <summary>
        /// Optional: Save Windows GUID
        /// </summary>
        public string GUID { get; set; }

        /// <summary>
        /// Wi-Fi Accesspoint's ESSID
        /// </summary>
        public string SSID { get; set; }

        /// <summary>
        /// Is Open Access Point? (= no security)
        /// </summary>
        public bool Open { get; set; }

        /// <summary>
        /// Is Enterprise Access Point? (like WPA2 Enterprise)
        /// </summary>
        public bool OneX { get; set; }

        /// <summary>
        /// Domain (only when OneX = true)
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// Username (only when OneX = true)
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Password or Passphase (only on OneX = true or Open = false)
        /// </summary>
        public string Password { get; set; }

        public string AuthType
        {
            get
            {
                if (Open)
                    return "Open";
                else if (!OneX)
                    return "Personal";
                else
                    return "Enterprise";
            }
        }

        public override string ToString()
        {
            if (Open)
                return SSID;
            else if (!OneX)
                return $"{SSID}: {Password}";
            else
            {
                if (Domain == string.Empty)
                    return $"{SSID}: {Username} : {Password}";
                else
                    return $"{SSID}: {Username}@{Domain} : {Password}";
            }
        }
    }
}