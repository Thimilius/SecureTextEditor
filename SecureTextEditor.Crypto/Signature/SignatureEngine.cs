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
        /// The key sizes accepted for normal DSA.
        /// </summary>
        public static readonly int[] DSA_ACCEPTED_KEYS = new int[] { 1024, 3072 };
        /// <summary>
        /// The key sizes accepted for ECDSA.
        /// </summary>
        public static readonly int[] ECDSA_ACCEPTED_KEYS = new int[] { 256 };
        /// <summary>
        /// The certainty for key genration.
        /// </summary>
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

        /// <summary>
        /// Creates a new signature engine with given parameters.
        /// </summary>
        /// <param name="type">The type of signature</param>
        /// <param name="keySize">The key size for the signature</param>
        public SignatureEngine(SignatureType type, int keySize) {
            VerifyParameters(type, keySize);

            m_Type = type;
            m_KeySize = keySize;
            m_Signer = GetSigner(type);
        }

        /// <summary>
        /// Signs an input with a given private key and returns the signature.
        /// </summary>
        /// <param name="input">The input to sign</param>
        /// <param name="privateKey">The private key to sign with</param>
        /// <returns>The generated signature</returns>
        public byte[] Sign(byte[] input, byte[] privateKey) {
            m_Signer.Init(true, GetPrivateParameters(privateKey));
            m_Signer.BlockUpdate(input, 0, input.Length);
            return m_Signer.GenerateSignature();
        }

        /// <summary>
        /// Verifys an input and signature with a given public key.
        /// </summary>
        /// <param name="input">The input to verify</param>
        /// <param name="signature">The signature to verify</param>
        /// <param name="publicKey">The public key to verify with</param>
        /// <returns>True if verification was successfull otherwise false</returns>
        public bool Verify(byte[] input, byte[] signature, byte[] publicKey) {
            m_Signer.Init(false, GetPublicParameters(publicKey));
            m_Signer.BlockUpdate(input, 0, input.Length);
            return m_Signer.VerifySignature(signature);
        }

        /// <summary>
        /// Generates a new private-public key pair.
        /// </summary>
        /// <returns>The generated key pair</returns>
        public SignatureKeyPair GenerateKeyPair() {
            AsymmetricCipherKeyPair keyPair = GetKeyPair();

            // Encode keys
            PrivateKeyInfo privateInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(keyPair.Private);
            SubjectPublicKeyInfo publicInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keyPair.Public);
            byte[] privateEncoded = privateInfo.GetEncoded();
            byte[] publicEncoded = publicInfo.GetEncoded();

            return new SignatureKeyPair(privateEncoded, publicEncoded, keyPair);
        }

        /// <summary>
        /// Gets the asymmetric unencoded key pair.
        /// </summary>
        /// <returns></returns>
        private AsymmetricCipherKeyPair GetKeyPair() {
            switch (m_Type) {
                case SignatureType.DSAWithSHA256:
                    // Generate parameters for key generation
                    DsaParametersGenerator parametersGenerator = new DsaParametersGenerator(new Sha256Digest());
                    parametersGenerator.Init(new DsaParameterGenerationParameters(m_KeySize, GetParameterN(m_KeySize), CERTAINTY, new SecureRandom()));
                    var parameters = parametersGenerator.GenerateParameters();
                    
                    // Generate key pair
                    DsaKeyPairGenerator dsaGenerator = new DsaKeyPairGenerator();
                    dsaGenerator.Init(new DsaKeyGenerationParameters(new SecureRandom(), parameters));
                    return dsaGenerator.GenerateKeyPair();
                case SignatureType.ECDSAWithSHA256:
                    // Generator key pair for EC P-256
                    ECKeyPairGenerator ecGenerator = new ECKeyPairGenerator();
                    ecGenerator.Init(new KeyGenerationParameters(new SecureRandom(), m_KeySize));
                    return ecGenerator.GenerateKeyPair();
                default: throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Gets the signer for the given signature type.
        /// </summary>
        /// <param name="type">The signature type</param>
        /// <returns>The signer</returns>
        private ISigner GetSigner(SignatureType type) {
            switch (type) {
                case SignatureType.DSAWithSHA256: return new DsaDigestSigner(new DsaSigner(), new Sha256Digest());
                case SignatureType.ECDSAWithSHA256: return new DsaDigestSigner(new ECDsaSigner(), new Sha256Digest());
                default: throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Gets the parameters for a private key.
        /// </summary>
        /// <param name="privateKey">The private key</param>
        /// <returns>The private key parameters</returns>
        private ICipherParameters GetPrivateParameters(byte[] privateKey) {
            return PrivateKeyFactory.CreateKey(privateKey);
        }

        /// <summary>
        /// Gets the parameters for a public key.
        /// </summary>
        /// <param name="publicKey">The public key</param>
        /// <returns>The public key parameters</returns>
        private ICipherParameters GetPublicParameters(byte[] publicKey) {
            return PublicKeyFactory.CreateKey(publicKey);
        }

        /// <summary>
        /// Converts key size to the parameter n for key generation.
        /// </summary>
        /// <param name="keySize">The key size</param>
        /// <returns>The parameter n</returns>
        int GetParameterN(int keySize) {
            switch (keySize) {
                case 1024: return 160;
                case 3072: return 256;
                default: throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Validates the engine configuration based on given parameters.
        /// </summary>
        /// <param name="type">The type of signature</param>
        /// <param name="keySize">The key size</param>
        private void VerifyParameters(SignatureType type, int keySize) {
            switch (type) {
                case SignatureType.None:
                    throw new InvalidOperationException();
                case SignatureType.DSAWithSHA256:
                    if (!DSA_ACCEPTED_KEYS.Contains(keySize)) {
                        throw new InvalidOperationException($"Invalid key size of {keySize}!");
                    }
                    break;
                case SignatureType.ECDSAWithSHA256:
                    if (!ECDSA_ACCEPTED_KEYS.Contains(keySize)) {
                        throw new InvalidOperationException($"Invalid key size of {keySize}!");
                    }
                    break;
            }
        }
    }
}
