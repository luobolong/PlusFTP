namespace Hani.Utilities
{
    public sealed class UserInfo
    {
        public string Host { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        private int _port = 21;
        public int Port { get { return _port; } set { _port = value; } }

        public int Encryption { get; set; }
        public int Protocol { get; set; }
        public int UTF8 { get; set; }
        public int MODEZ { get; set; }
        public int Proxy { get; set; }
        public int Cache { get; set; }
        public bool Selected { get; set; }

        public override string ToString()
        {
            if (Host.NullEmpty()) return string.Empty;

            return ((UserName.NullEmpty() ? string.Empty : UserName + "@") + Host + ":" + Port);
        }

        public string FullString()
        {
            if (Host.NullEmpty()) return string.Empty;

            return getString("Host", Host) +
                   (UserName.NullEmpty() ? string.Empty : getString("UserName", UserName)) +
                   (Password.NullEmpty() ? string.Empty : getString("Password", Password)) +
                   (Port == 21 ? string.Empty : getString("Port", Port)) +
                   (Encryption == 0 ? string.Empty : getString("Encryption", Encryption)) +
                   (Protocol == 0 ? string.Empty : getString("Protocol", Protocol)) +
                   (UTF8 == 0 ? string.Empty : getString("UTF8", UTF8)) +
                   (MODEZ == 0 ? string.Empty : getString("MODEZ", MODEZ)) +
                   (Proxy == 0 ? string.Empty : getString("Proxy", Proxy)) +
                   (Cache == 0 ? string.Empty : getString("Cache", Cache)) +
                   (Selected ? getString("Selected", Selected) : string.Empty);
        }

        private static string getString(string name, object o)
        {
            return name + "\"" + o.ToString() + "\"" + " ";
        }
    }
}