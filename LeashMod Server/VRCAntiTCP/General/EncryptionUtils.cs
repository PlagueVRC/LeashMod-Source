using System;
using System.Security.Cryptography;

namespace VRCAntiTCP.General
{
	public class EncryptionUtils
	{
		public static byte[] GetRandomBytes(int length, bool addByte)
		{
			if (addByte && length > 255)
			{
				//throw new ArgumentException("Length must be 1 byte <256");
			}
			byte[] array = new byte[length + (addByte ? 1 : 0)];
			EncryptionUtils.rng.GetBytes(array);
			if (addByte)
			{
				array[0] = (byte)length;
			}
			return array;
		}

		// Note: this type is marked as 'beforefieldinit'.
		static EncryptionUtils()
		{
		}

		public EncryptionUtils()
		{
		}

		private static RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
	}
}
