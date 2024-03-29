﻿using System;
using System.Security.Cryptography;

namespace VRCAntiTCP.Cryptography
{
	public abstract class BaseCrypto : ICryptoTransform, IDisposable
	{
		public int InputBlockSize
		{
			get
			{
				return 1;
			}
		}

		public int OutputBlockSize
		{
			get
			{
				return 1;
			}
		}

		public bool CanTransformMultipleBlocks
		{
			get
			{
				return true;
			}
		}

		public bool CanReuseTransform
		{
			get
			{
				return true;
			}
		}

		protected BaseCrypto(byte[] key)
		{
			if (key.Length == 0)
			{
				//throw new ArgumentException("Must provide a key");
			}
			this.key = key;
			this.currentKey = 0;
			for (int i = 0; i < key.Length; i++)
			{
				this.currentKey += key[i];
			}
		}

		protected abstract byte DoByte(byte b);

		public int TransformBlock(byte[] from, int frominx, int len, byte[] to, int toinx)
		{
			for (int i = 0; i < len; i++)
			{
				byte b = this.currentKey;
				to[toinx + i] = this.DoByte(from[frominx + i]);
				this.BumpKey();
			}
			return len;
		}

		public byte[] TransformFinalBlock(byte[] from, int frominx, int len)
		{
			byte[] array = new byte[len];
			this.TransformBlock(from, frominx, len, array, 0);
			return array;
		}

		protected void BumpKey()
		{
			this.keyinx = (this.keyinx + 1) % this.key.Length;
			this.currentKey = BaseCrypto.Multiply257(this.key[this.keyinx], this.currentKey);
		}

		protected static byte Multiply257(byte a, byte b)
		{
			return (byte)((int)((a + 1) * (b + 1)) % 257 - 1);
		}

		protected static byte Complement257(byte b)
		{
			return BaseCrypto.complements[(int)b];
		}

		public void Dispose()
		{
		}

		// Note: this type is marked as 'beforefieldinit'.
		static BaseCrypto()
		{
		}

		protected byte[] key;

		protected byte currentKey;

		protected int done = 0;

		protected int keyinx = 0;

		protected static byte[] complements = new byte[]
		{
			0,
			128,
			85,
			192,
			102,
			42,
			146,
			224,
			199,
			179,
			186,
			149,
			177,
			201,
			119,
			240,
			120,
			99,
			229,
			89,
			48,
			221,
			189,
			74,
			71,
			88,
			237,
			100,
			194,
			59,
			198,
			248,
			147,
			188,
			234,
			49,
			131,
			114,
			144,
			44,
			162,
			152,
			5,
			110,
			39,
			94,
			174,
			165,
			20,
			35,
			125,
			172,
			96,
			118,
			242,
			178,
			247,
			225,
			60,
			29,
			58,
			227,
			101,
			252,
			86,
			73,
			233,
			222,
			148,
			245,
			180,
			24,
			168,
			65,
			23,
			185,
			246,
			200,
			243,
			150,
			164,
			209,
			95,
			204,
			126,
			2,
			64,
			183,
			25,
			19,
			208,
			175,
			151,
			215,
			45,
			82,
			52,
			138,
			134,
			17,
			27,
			62,
			4,
			214,
			163,
			176,
			244,
			187,
			223,
			249,
			43,
			217,
			115,
			123,
			37,
			112,
			133,
			158,
			53,
			14,
			16,
			157,
			139,
			113,
			219,
			50,
			84,
			254,
			1,
			171,
			205,
			36,
			142,
			116,
			98,
			239,
			241,
			202,
			97,
			122,
			143,
			218,
			132,
			140,
			38,
			212,
			6,
			32,
			68,
			11,
			79,
			92,
			41,
			251,
			193,
			228,
			238,
			121,
			117,
			203,
			173,
			210,
			40,
			104,
			80,
			47,
			236,
			230,
			72,
			191,
			253,
			129,
			51,
			160,
			46,
			91,
			105,
			12,
			55,
			9,
			70,
			232,
			190,
			87,
			231,
			75,
			10,
			107,
			33,
			22,
			182,
			169,
			3,
			154,
			28,
			197,
			226,
			195,
			30,
			8,
			77,
			13,
			137,
			159,
			83,
			130,
			220,
			235,
			90,
			81,
			161,
			216,
			145,
			250,
			103,
			93,
			211,
			111,
			141,
			124,
			206,
			21,
			67,
			108,
			7,
			57,
			196,
			61,
			155,
			18,
			167,
			184,
			181,
			66,
			34,
			207,
			166,
			26,
			156,
			135,
			15,
			136,
			54,
			78,
			106,
			69,
			76,
			56,
			31,
			109,
			213,
			153,
			63,
			170,
			127,
			byte.MaxValue
		};
	}
}
