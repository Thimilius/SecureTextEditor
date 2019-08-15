using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using SecureTextEditor.Crypto.Signature;
using System;
using System.IO;

namespace SecureTextEditor.Crypto {
    // TODO: Put the storage class into own namespace?

    /// <summary>
    /// Abstracts a PKCS12 key storage for DSA signature key pairs.
    /// </summary>
    public class SignatureKeyStorage {
        

        /// <summary>
        /// The certificate distinguished name.
        /// </summary>
        private static readonly X509Name CERTIFICATE_DISTINGUISHED_NAME = new X509Name("CN=SecureTextEditor");

        /// <summary>
        /// The file path to the storage.
        /// </summary>
        private readonly string m_FilePath;
        /// <summary>
        /// The actual storage.
        /// </summary>
        private readonly Pkcs12Store m_Store;

        /// <summary>
        /// Creates a new key storage at a given path.
        /// </summary>
        /// <param name="filePath"></param>
        public SignatureKeyStorage(string filePath) {
            m_Store = new Pkcs12Store();
            m_FilePath = filePath;
        }

        /// <summary>
        /// Loads the storage into memory.
        /// </summary>
        /// <param name="password">The password of the storage</param>
        public KeyStorageLoadStatus Load(char[] password) {
            // This inital loading step may be better in a seperate function
            try {
                if (File.Exists(m_FilePath)) {
                    using (FileStream stream = new FileStream(m_FilePath, FileMode.Open)) {
                        m_Store.Load(stream, password);
                    }
                }
                return KeyStorageLoadStatus.Success;
            } catch (IOException e) {
                if (e.Message == "PKCS12 key store MAC invalid - wrong password or corrupted file.") {
                    return KeyStorageLoadStatus.PasswordWrong;
                } else {
                    return KeyStorageLoadStatus.Failed;
                }
            } catch {
                return KeyStorageLoadStatus.Failed;
            }
        }

        /// <summary>
        /// Saves the storage to disk.
        /// </summary>
        /// <param name="password">The password for the storage</param>
        public void Save(char[] password) {
            using (FileStream stream = new FileStream(m_FilePath, FileMode.Create)) {
                m_Store.Save(stream, password, new SecureRandom());
            }
        }

        /// <summary>
        /// Checks whether or not an alias exits in the storage.
        /// </summary>
        /// <param name="alias">The alias to check</param>
        /// <returns>True if the alias exists otherwise false</returns>
        public bool Exists(string alias) {
            return m_Store.ContainsAlias(alias);
        }

        /// <summary>
        /// Stores the private key of a signature key pair in the storage.
        /// </summary>
        /// <param name="alias">The alias to save the private key at</param>
        /// <param name="pair">The signature key pair with the private key to save</param>
        public void Store(string alias, SignatureKeyPair pair) {
            AsymmetricCipherKeyPair ecKeyPair = GenerateECKeyPair();
            X509Certificate certificate = GenerateCertificate(ecKeyPair.Private, ecKeyPair.Public);

            X509CertificateEntry[] certificates = new X509CertificateEntry[] { new X509CertificateEntry(certificate) };

            AsymmetricKeyEntry privateEntry = new AsymmetricKeyEntry(pair.Pair.Private);
            m_Store.SetKeyEntry(alias, privateEntry, certificates);
        }

        /// <summary>
        /// Retrieves the private key pair out of storage and returns the with the combined signature key pair.
        /// </summary>
        /// <param name="alias">The alias to load the private key from</param>
        /// <returns>The combined signature key pair</returns>
        public SignatureKeyPair Retrieve(string alias) {
            // Currently we always assume we are dealing with DSA
            if (m_Store.GetKey(alias).Key is DsaPrivateKeyParameters privateParameters) {
                // Get public key from private
                BigInteger p = privateParameters.Parameters.P;
                BigInteger g = privateParameters.Parameters.G;
                BigInteger x = privateParameters.X;
                BigInteger y = g.ModPow(x, p);
                DsaPublicKeyParameters publicKeyParameters = new DsaPublicKeyParameters(y, privateParameters.Parameters);

                AsymmetricCipherKeyPair keyPair = new AsymmetricCipherKeyPair(publicKeyParameters, privateParameters);

                PrivateKeyInfo privateInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(privateParameters);
                SubjectPublicKeyInfo publicInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keyPair.Public);
                byte[] privateEncoded = privateInfo.GetEncoded();
                byte[] publicEncoded = publicInfo.GetEncoded();

                return new SignatureKeyPair(privateEncoded, publicEncoded, keyPair);
            } else {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Generates a new certificate.
        /// </summary>
        /// <param name="privateKey">The private key parameter</param>
        /// <param name="publicKey">The public key parameter</param>
        /// <returns>The generated certificate</returns>
        private X509Certificate GenerateCertificate(AsymmetricKeyParameter privateKey, AsymmetricKeyParameter publicKey) {
            ISignatureFactory factory = new Asn1SignatureFactory(X9ObjectIdentifiers.ECDsaWithSha256.ToString(), privateKey);

            X509V3CertificateGenerator generator = new X509V3CertificateGenerator();
            generator.SetIssuerDN(CERTIFICATE_DISTINGUISHED_NAME);
            generator.SetSubjectDN(CERTIFICATE_DISTINGUISHED_NAME);
            generator.SetSerialNumber(BigInteger.ValueOf(1));
            generator.SetNotAfter(DateTime.Now.AddYears(1));
            generator.SetNotBefore(DateTime.UtcNow);
            generator.SetPublicKey(publicKey);

            return generator.Generate(factory);
        }

        /// <summary>
        /// Generates a new elliptic curve pair.
        /// </summary>
        /// <returns>The generated elliptic curve pair</returns>
        private static AsymmetricCipherKeyPair GenerateECKeyPair() {
            X9ECParameters cureParameters = SecNamedCurves.GetByName("SECP256R1");
            ECDomainParameters domain = new ECDomainParameters(cureParameters.Curve, cureParameters.G, cureParameters.N);
            ECKeyGenerationParameters generationParameters = new ECKeyGenerationParameters(domain, new SecureRandom());

            ECKeyPairGenerator keyGenerator = new ECKeyPairGenerator();
            keyGenerator.Init(generationParameters);

            return keyGenerator.GenerateKeyPair();
        }
    }
}
