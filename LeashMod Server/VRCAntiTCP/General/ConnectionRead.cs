using System;
using System.Threading.Tasks;

namespace VRCAntiTCP.General
{
	public delegate Task ConnectionRead(ClientInfo ci, string text);
}
