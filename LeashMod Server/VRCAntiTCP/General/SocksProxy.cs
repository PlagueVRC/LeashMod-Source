using System;
using System.Net;

namespace VRCAntiTCP.General
{
	public struct SocksProxy
	{
		public SocksProxy(string hostname, ushort port, string username, string password)
		{
			this.port = port;
			this.host = Dns.GetHostEntry(hostname).AddressList[0];
			this.username = username;
			this.password = password;
		}

		public IPAddress host;

		public ushort port;

		public string username;

		public string password;
	}
}
