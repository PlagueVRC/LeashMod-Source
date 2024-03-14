#if !Free

namespace VRCAntiTCP.Cryptography
{
    public class SimpleEncryptor : BaseCrypto
    {
        internal SimpleEncryptor(byte[] key)
            : base(key)
        {
        }

        protected override byte DoByte(byte b)
        {
            byte b2 = BaseCrypto.Multiply257((byte)(b + currentKey), currentKey);
            currentKey = BaseCrypto.Multiply257((byte)(b + b2), currentKey);
            return b2;
        }
    }
}
#endif