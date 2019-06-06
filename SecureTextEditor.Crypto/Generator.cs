using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using System;

namespace SecureTextEditor.Crypto {
    // TODO: Do we actually need this class anymore?
    /// <summary>
    /// Simple internal class used to abstract key generation.
    /// </summary>
    internal class Generator {
        internal static byte[] GenerateKey(KeyType type, int keySize, char[] password) {
            switch (type) {
                case KeyType.Generated:
                    CipherKeyGenerator generator = new CipherKeyGenerator();
                    generator.Init(new KeyGenerationParameters(new SecureRandom(), keySize));
                    return generator.GenerateKey();
                case KeyType.PBE:
                    return PbeParametersGenerator.Pkcs12PasswordToBytes(password);
                case KeyType.PBEWithSCRYPT:
                    return PbeParametersGenerator.Pkcs5PasswordToUtf8Bytes(password);
                default: throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
