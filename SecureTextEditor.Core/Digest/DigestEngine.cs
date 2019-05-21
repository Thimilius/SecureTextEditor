using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Utilities.Encoders;
using System;

namespace SecureTextEditor.Core.Digest {
    public class DigestEngine {
        private static readonly byte[] MAC_KEY = Hex.Decode("000102030405060708090a0b0c0d0e0f");

        private DigestType m_Type;

        public DigestEngine(DigestType type) {
            m_Type = type;
        }

        public byte[] Digest(byte[] input) {
            switch (m_Type) {
                case DigestType.SHA256: return DigestSHA256(input);
                case DigestType.AESCMAC: return DigestAESCMAC(input);
                case DigestType.HMACSHA256: return DigestHMACSHA256(input);
                default: throw new InvalidOperationException();
            }
        }

        private byte[] DigestSHA256(byte[] input) {
            IDigest digest = new Sha256Digest();
            digest.BlockUpdate(input, 0, input.Length);
            byte[] output = new byte[digest.GetDigestSize()];
            digest.DoFinal(output, 0);
            return output;
        }

        private byte[] DigestAESCMAC(byte[] input) {
            IMac mac = new CMac(new AesEngine());
            mac.Init(new KeyParameter(MAC_KEY));
            mac.BlockUpdate(input, 0, input.Length);
            byte[] output = new byte[mac.GetMacSize()];
            mac.DoFinal(output, 0);
            return output;
        }

        private byte[] DigestHMACSHA256(byte[] input) {
            IMac mac = new HMac(new Sha256Digest());
            mac.Init(new KeyParameter(MAC_KEY));
            mac.BlockUpdate(input, 0, input.Length);
            byte[] output = new byte[mac.GetMacSize()];
            mac.DoFinal(output, 0);
            return output;
        }
    }
}
