using System;

namespace VRCAntiTCP.General
{
	[Serializable]
	public struct Parameter
	{
		public Parameter(byte[] content, byte type)
		{
			this.content = content;
			this.Type = type;
		}

		public byte Type;

		public byte[] content;
	}
}
