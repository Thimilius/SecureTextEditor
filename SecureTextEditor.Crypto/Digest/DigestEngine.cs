using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Encoders;
using System;

namespace SecureTextEditor.Crypto.Digest {
    // NOTE: Macs should usually not be used
    // because of the seperate key that is needed
    public class DigestEngine {
        private const int MAC_KEY_SIZE = 256;

        private DigestType m_Type;
        private IDigest m_Digest;
        private IMac m_Mac;

        public DigestEngine(DigestType type) {
            m_Type = type;
            if (IsMacConfigured()) {
                m_Mac = GetMac(type);
            } else {
                m_Digest = GetDigest(type);
            }
        }

        public byte[] Digest(byte[] input, byte[] key) {
            switch (m_Type) {
                case DigestType.SHA256: return DigestHash(input);
                case DigestType.AESCMAC: 
                case DigestType.HMACSHA256: return DigestMac(input, key);
                default: throw new InvalidOperationException();
            }
        }

        public byte[] GenerateKey() {
            if (IsMacConfigured()) {
                return Generator.GenerateKey(MAC_KEY_SIZE);
            } else {
                return null;
            }
        }

        public int GetDigestLength() {
            if (IsMacConfigured()) {
                return m_Mac.GetMacSize();
            } else {
                return m_Digest.GetDigestSize();
            }
        }

        private byte[] DigestHash(byte[] input) {
            m_Digest.BlockUpdate(input, 0, input.Length);
            byte[] output = new byte[m_Digest.GetDigestSize()];
            m_Digest.DoFinal(output, 0);
            return output;
        }

        private byte[] DigestMac(byte[] input, byte[] key) {
            m_Mac.Init(new KeyParameter(key));
            m_Mac.BlockUpdate(input, 0, input.Length);
            byte[] output = new byte[m_Mac.GetMacSize()];
            m_Mac.DoFinal(output, 0);
            return output;
        }

        private IDigest GetDigest(DigestType type) {
            switch (type) {
                case DigestType.SHA256: return new Sha256Digest();
                default: throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        private IMac GetMac(DigestType type) {
            switch (type) {
                case DigestType.AESCMAC: return new CMac(new AesEngine());
                case DigestType.HMACSHA256: return new HMac(new Sha256Digest());
                default: throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        private bool IsMacConfigured() {
            return m_Type != DigestType.SHA256;
        }

        public static bool AreEqual(byte[] a, byte[] b) {
            return Org.BouncyCastle.Utilities.Arrays.ConstantTimeAreEqual(a, b);
        }
    }
}
