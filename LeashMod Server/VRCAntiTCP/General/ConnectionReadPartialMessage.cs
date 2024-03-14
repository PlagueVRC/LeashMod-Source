using System;

namespace VRCAntiTCP.General
{
	public delegate void ConnectionReadPartialMessage(ClientInfo ci, uint code, byte[] bytes, int start, int read, int sofar, int totallength);
}
