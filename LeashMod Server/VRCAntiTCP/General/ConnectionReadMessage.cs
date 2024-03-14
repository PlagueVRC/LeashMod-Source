using System;
using System.Threading.Tasks;

namespace VRCAntiTCP.General
{
	public delegate Task ConnectionReadMessage(ClientInfo ci, uint code, byte[] bytes, int len);
}
