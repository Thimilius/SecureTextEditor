using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureTextEditor.Crypto.Signature {
    /// <summary>
    /// Represents a key pair used in signatures.
    /// </summary>
    public class SignatureKeyPair {
        /// <summary>
        /// Gets the private key.
        /// </summary>
        public byte[] PrivateKey { get; }
        /// <summary>
        /// Gets the public key.
        /// </summary>
        public byte[] PublicKey { get; }

        /// <summary>
        /// Generates
        /// </summary>
        /// <param name="privateKey"></param>
        /// <param name="publicKey"></param>
        public SignatureKeyPair(byte[] privateKey, byte[] publicKey) {
            PrivateKey = privateKey;
            PublicKey = publicKey;
        }
    }
}
