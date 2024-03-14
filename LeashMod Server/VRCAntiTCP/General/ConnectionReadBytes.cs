using System;
using System.Threading.Tasks;

namespace VRCAntiTCP.General
{
	public delegate Task ConnectionReadBytes(ClientInfo ci, byte[] bytes, int len);
}
