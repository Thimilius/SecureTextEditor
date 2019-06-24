using System;
using System.Linq;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace SecureTextEditor.Crypto.Signature {
    /// <summary>
    /// Signature engine abstracting signature algorithms.
    /// </summary>
    public class SignatureEngine {
        /// <summary>
        /// The key sizes accepted for key generation.
        /// </summary>
        public static readonly int[] ACCEPTED_KEYS = new int[] { 1024, 3072 };

        private const int CERTAINTY = 80;

        /// <summary>
        /// The signature type that is used.
        /// </summary>
        private readonly SignatureType m_Type;
        /// <summary>
        /// The size of the key to use.
        /// </summary>
        private readonly int m_KeySize;
        /// <summary>
        /// The concrete signer that will be used for signing and verifying.
        /// </summary>
        private readonly ISigner m_Signer;

        public SignatureEngine(SignatureType type, int keySize) {
            VerifyParameters(type, keySize);

            m_Type = type;
            m_KeySize = keySize;
            m_Signer = GetSigner(type);
        }

        public byte[] Sign(byte[] input, byte[] privateKey) {
            m_Signer.Init(true, GetPrivateParameters(privateKey));
            m_Signer.BlockUpdate(input, 0, input.Length);
            return m_Signer.GenerateSignature();
        }

        public bool Verify(byte[] input, byte[] signature, byte[] publicKey) {
            m_Signer.Init(false, GetPublicParameters(publicKey));
            m_Signer.BlockUpdate(input, 0, input.Length);
            return m_Signer.VerifySignature(signature);
        }

        public SignatureKeyPair GenerateKeyPair() {
            AsymmetricCipherKeyPair keyPair = GetKeyPair();

            // Encode keys
            PrivateKeyInfo privateInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(keyPair.Private);
            SubjectPublicKeyInfo publicInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keyPair.Public);
            byte[] privateEncoded = privateInfo.GetEncoded();
            byte[] publicEncoded = publicInfo.GetEncoded();

            return new SignatureKeyPair(privateEncoded, publicEncoded);
        }

        private AsymmetricCipherKeyPair GetKeyPair() {
            switch (m_Type) {
                case SignatureType.SHA256WithDSA:
                    DsaKeyPairGenerator generator = new DsaKeyPairGenerator();
                    DsaParametersGenerator parametersGenerator = new DsaParametersGenerator();

                    parametersGenerator.Init(m_KeySize, CERTAINTY, new SecureRandom());

                    var parameters = parametersGenerator.GenerateParameters();
                    generator.Init(new DsaKeyGenerationParameters(new SecureRandom(), parameters));

                    return generator.GenerateKeyPair();
                default: throw new InvalidOperationException();
            }
        }

        private ISigner GetSigner(SignatureType type) {
            switch (type) {
                case SignatureType.SHA256WithDSA: return new DsaDigestSigner(new DsaSigner(), new Sha256Digest());
                default: throw new InvalidOperationException();
            }
        }

        private ICipherParameters GetPrivateParameters(byte[] privateKey) {
            return PrivateKeyFactory.CreateKey(privateKey);
        }

        private ICipherParameters GetPublicParameters(byte[] publicKey) {
            return PublicKeyFactory.CreateKey(publicKey);
        }

        private void VerifyParameters(SignatureType type, int keySize) {
            if (!ACCEPTED_KEYS.Contains(keySize)) {
                throw new InvalidOperationException($"Invalid key size of {keySize}!");
            }
        }
    }
}
