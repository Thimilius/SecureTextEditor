using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;

namespace SecureTextEditor.Crypto {
    /// <summary>
    /// Simple internal class used to abstract key generation.
    /// </summary>
    internal class Generator {
        /// <summary>
        /// Generates a cryptographic key with given size in bits.
        /// </summary>
        /// <param name="keySize">The key size in bits</param>
        /// <returns>The generated cryptographic key</returns>
        internal static byte[] GenerateKey(int keySize) {
            CipherKeyGenerator generator = new CipherKeyGenerator();
            generator.Init(new KeyGenerationParameters(new SecureRandom(), keySize));
            return generator.GenerateKey();
        }
    }
}
