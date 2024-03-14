#if !Free

namespace VRCAntiTCP.Cryptography
{
    public class SimpleDecryptor : BaseCrypto
    {
        internal SimpleDecryptor(byte[] key)
            : base(key)
        {
        }

        protected override byte DoByte(byte b)
        {
            byte b2 = (byte)(BaseCrypto.Multiply257(b, BaseCrypto.Complement257(currentKey)) - currentKey);
            currentKey = BaseCrypto.Multiply257((byte)(b + b2), currentKey);
            return b2;
        }
    }
}
#endif