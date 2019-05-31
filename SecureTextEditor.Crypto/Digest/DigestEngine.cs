using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using System;

namespace SecureTextEditor.Crypto.Digest {
    /// <summary>
    /// Digest engine abstracting different hashing algorithms.
    /// </summary>
    public class DigestEngine {
        /// <summary>
        /// The size of the mac key in bits.
        /// </summary>
        private const int MAC_KEY_SIZE = 256;

        /// <summary>
        /// The digest type to use.
        /// </summary>
        private readonly DigestType m_Type;
        /// <summary>
        /// The actual digest that will be used.
        /// </summary>
        private readonly IDigest m_Digest;
        /// <summary>
        /// The actual mac that will be used.
        /// </summary>
        private readonly IMac m_Mac;

        /// <summary>
        /// Creates a new digest engine with given parameters.
        /// </summary>
        /// <param name="type">The digest type to use</param>
        public DigestEngine(DigestType type) {
            m_Type = type;
            if (IsMacConfigured()) {
                m_Mac = GetMac(type);
            } else {
                m_Digest = GetDigest(type);
            }
        }

        /// <summary>
        /// Hashes a given input and returns the hash result.
        /// </summary>
        /// <param name="input">The input that will get hashed</param>
        /// <param name="key">The key to use (Can be null if not needed)</param>
        /// <returns></returns>
        public byte[] Digest(byte[] input, byte[] key) {
            switch (m_Type) {
                case DigestType.SHA256: return DigestHash(input);
                case DigestType.AESCMAC: 
                case DigestType.HMACSHA256: return DigestMac(input, key);
                default: throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Generates a key for use with this engine.
        /// </summary>
        /// <returns>The generated key</returns>
        public byte[] GenerateKey() {
            if (IsMacConfigured()) {
                return Generator.GenerateKey(MAC_KEY_SIZE);
            } else {
                return null;
            }
        }

        /// <summary>
        /// Gets the length of the hash that will be returned from the digest operation.
        /// </summary>
        /// <returns>The length returned by the digest operation</returns>
        public int GetDigestLength() {
            if (IsMacConfigured()) {
                return m_Mac.GetMacSize();
            } else {
                return m_Digest.GetDigestSize();
            }
        }

        /// <summary>
        /// Hashes a given input with a normal hash algorithm.
        /// </summary>
        /// <param name="input">The input to hash</param>
        /// <returns>The hash result</returns>
        private byte[] DigestHash(byte[] input) {
            m_Digest.BlockUpdate(input, 0, input.Length);
            byte[] output = new byte[m_Digest.GetDigestSize()];
            m_Digest.DoFinal(output, 0);
            return output;
        }

        /// <summary>
        /// Hashes a given input with a mac algorithm.
        /// </summary>
        /// <param name="input">The input to hash</param>
        /// <param name="key">The key to use</param>
        /// <returns>The hash result</returns>
        private byte[] DigestMac(byte[] input, byte[] key) {
            m_Mac.Init(new KeyParameter(key));
            m_Mac.BlockUpdate(input, 0, input.Length);
            byte[] output = new byte[m_Mac.GetMacSize()];
            m_Mac.DoFinal(output, 0);
            return output;
        }

        /// <summary>
        /// Converts the digest type to a normal hash algorithm to use.
        /// </summary>
        /// <param name="type">The type of digest</param>
        /// <returns>The normal hash algorithm</returns>
        private IDigest GetDigest(DigestType type) {
            switch (type) {
                case DigestType.SHA256: return new Sha256Digest();
                default: throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        /// <summary>
        /// Converts the digest type to a mac algorithm to use.
        /// </summary>
        /// <param name="type">The type of digest</param>
        /// <returns>The mac algorithm</returns>
        private IMac GetMac(DigestType type) {
            switch (type) {
                case DigestType.AESCMAC: return new CMac(new AesEngine());
                case DigestType.HMACSHA256: return new HMac(new Sha256Digest());
                default: throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        /// <summary>
        /// Checks whether or not the engine is configured to use a mac algorithm
        /// </summary>
        /// <returns>True if the engine is configured to use a mac algorithm otherwise false</returns>
        private bool IsMacConfigured() {
            return m_Type != DigestType.SHA256;
        }

        /// <summary>
        /// Checks whether or not the content of two given arrays are the same.
        /// </summary>
        /// <param name="a">The first array</param>
        /// <param name="b">The second array</param>
        /// <returns>True if the two arrays have the same content otherwise false</returns>
        public static bool AreEqual(byte[] a, byte[] b) {
            return Org.BouncyCastle.Utilities.Arrays.ConstantTimeAreEqual(a, b);
        }
    }
}
