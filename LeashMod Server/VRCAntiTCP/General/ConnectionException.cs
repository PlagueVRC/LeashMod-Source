using System;

namespace VRCAntiTCP.General
{
	public class ConnectionException : Exception
	{
		public ConnectionException(string message) : base(message)
		{
		}
	}
}
