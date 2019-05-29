using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;

namespace SecureTextEditor.Crypto {
    internal class Generator {
        internal static byte[] GenerateKey(int keySize) {
            CipherKeyGenerator generator = new CipherKeyGenerator();
            generator.Init(new KeyGenerationParameters(new SecureRandom(), keySize));
            return generator.GenerateKey();
        }
    }
}
