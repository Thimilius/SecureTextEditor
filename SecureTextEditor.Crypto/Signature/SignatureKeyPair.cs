using Org.BouncyCastle.Crypto;

namespace SecureTextEditor.Crypto.Signature {
    /// <summary>
    /// Represents a key pair used in signatures.
    /// </summary>
    public class SignatureKeyPair {
        /// <summary>
        /// Gets the encoded private key.
        /// </summary>
        public byte[] PrivateKey { get; }
        /// <summary>
        /// Gets the encoded public key.
        /// </summary>
        public byte[] PublicKey { get; }
        /// <summary>
        /// The key pair used internally.
        /// </summary>
        internal AsymmetricCipherKeyPair Pair { get; }

        /// <summary>
        /// Generates
        /// </summary>
        /// <param name="privateKey"></param>
        /// <param name="publicKey"></param>
        internal SignatureKeyPair(byte[] privateKey, byte[] publicKey, AsymmetricCipherKeyPair pair) {
            PrivateKey = privateKey;
            PublicKey = publicKey;
            Pair = pair;
        }

        /// <summary>
        /// Clears out the key pairs.
        /// </summary>
        public void Clear() {
            PrivateKey.Clear();
            PublicKey.Clear();
        }
    }
}
