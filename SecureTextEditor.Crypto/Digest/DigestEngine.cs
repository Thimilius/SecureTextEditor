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

        public DigestEngine(DigestType type) {
            m_Type = type;
        }

        public byte[] Digest(byte[] input, byte[] key) {
            switch (m_Type) {
                case DigestType.SHA256: return DigestSHA256(input);
                case DigestType.AESCMAC: return DigestAESCMAC(input, key);
                case DigestType.HMACSHA256: return DigestHMACSHA256(input, key);
                default: throw new InvalidOperationException();
            }
        }

        public byte[] GenerateKey() {
            if (m_Type == DigestType.SHA256) {
                return null;
            } else {
                return Generator.GenerateKey(MAC_KEY_SIZE);
            }
        }

        private byte[] DigestSHA256(byte[] input) {
            IDigest digest = new Sha256Digest();
            digest.BlockUpdate(input, 0, input.Length);
            byte[] output = new byte[digest.GetDigestSize()];
            digest.DoFinal(output, 0);
            return output;
        }

        private byte[] DigestAESCMAC(byte[] input, byte[] key) {
            IMac mac = new CMac(new AesEngine());
            mac.Init(new KeyParameter(key));
            mac.BlockUpdate(input, 0, input.Length);
            byte[] output = new byte[mac.GetMacSize()];
            mac.DoFinal(output, 0);
            return output;
        }

        private byte[] DigestHMACSHA256(byte[] input, byte[] key) {
            IMac mac = new HMac(new Sha256Digest());
            mac.Init(new KeyParameter(key));
            mac.BlockUpdate(input, 0, input.Length);
            byte[] output = new byte[mac.GetMacSize()];
            mac.DoFinal(output, 0);
            return output;
        }

        public static bool AreEqual(byte[] a, byte[] b) {
            return Org.BouncyCastle.Utilities.Arrays.ConstantTimeAreEqual(a, b);
        }
    }
}
