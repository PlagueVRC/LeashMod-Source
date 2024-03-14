// VRCAntiTCP.Cryptography.SimpleDecryptor
using VRCAntiTCP.Cryptography;

namespace VRCAntiTCP.Cryptography
{
    public class SimpleDecryptor : BaseCrypto
    {
        public SimpleDecryptor(byte[] key)
            : base(key)
        {
        }

        protected override byte DoByte(byte b)
        {
            byte b2 = (byte) (BaseCrypto.Multiply257(b, BaseCrypto.Complement257(currentKey)) - currentKey);
            currentKey = BaseCrypto.Multiply257((byte) (b + b2), currentKey);
            return b2;
        }
    }
}