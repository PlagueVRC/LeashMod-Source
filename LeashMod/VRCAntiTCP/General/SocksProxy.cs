#if !Free
using System.Net;

namespace VRCAntiTCP.General
{
    internal struct SocksProxy
    {
        internal SocksProxy(string hostname, ushort port, string username, string password)
        {
            this.port = port;
            this.host = Dns.GetHostEntry(hostname).AddressList[0];
            this.username = username;
            this.password = password;
        }

        internal IPAddress host;

        internal ushort port;

        internal string username;

        internal string password;
    }
}
#endif