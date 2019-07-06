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
    /// <summary>
    /// Abstracts a PKCS12 key storage.
    /// </summary>
    public class SignatureKeyStorage {
        private const string KEY_STORE_ALIAS = "private_key";
        private const string KEY_STORE_FILEPATH = "storage.fks";
        private const string KEY_STORE_PASSWORD = "password";

        private static readonly X509Name CERTIFICATE_NAME = new X509Name("CN=KeyCA");

        private Pkcs12Store m_Store;

        public SignatureKeyStorage() {
            m_Store = new Pkcs12Store();

            // This inital loading step may be better in a seperate function
            if (File.Exists(KEY_STORE_FILEPATH)) {
                using (FileStream stream = new FileStream(KEY_STORE_FILEPATH, FileMode.Open)) {
                    m_Store.Load(stream, KEY_STORE_PASSWORD.ToCharArray());
                }
            }
        }

        public void Store(SignatureKeyPair pair) {
            AsymmetricCipherKeyPair ecKeyPair = GenerateEcKeyPair();
            X509Certificate certificate = GetCertificate(ecKeyPair.Private, ecKeyPair.Public);

            X509CertificateEntry[] certificates = new X509CertificateEntry[] { new X509CertificateEntry(certificate) };

            AsymmetricKeyEntry privateEntry = new AsymmetricKeyEntry(pair.Pair.Private);
            m_Store.SetKeyEntry(KEY_STORE_ALIAS, privateEntry, certificates);

            using (FileStream stream = new FileStream(KEY_STORE_FILEPATH, FileMode.Create)) {
                m_Store.Save(stream, KEY_STORE_PASSWORD.ToCharArray(), new SecureRandom());
            }
        }

        public bool Exists() {
            return m_Store.ContainsAlias(KEY_STORE_ALIAS);
        }

        public SignatureKeyPair Retrieve(byte[] publicKey) {
            AsymmetricKeyParameter privateParameter = m_Store.GetKey(KEY_STORE_ALIAS).Key;
            PrivateKeyInfo privateInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(privateParameter);
            byte[] privateEncoded = privateInfo.GetEncoded();
            return new SignatureKeyPair(privateEncoded, publicKey, new AsymmetricCipherKeyPair(PublicKeyFactory.CreateKey(publicKey), privateParameter));
        }

        private X509Certificate GetCertificate(AsymmetricKeyParameter privateKey, AsymmetricKeyParameter publicKey) {
            ISignatureFactory factory = new Asn1SignatureFactory(X9ObjectIdentifiers.ECDsaWithSha256.ToString(), privateKey);

            X509V3CertificateGenerator generator = new X509V3CertificateGenerator();
            generator.SetIssuerDN(CERTIFICATE_NAME);
            generator.SetSubjectDN(CERTIFICATE_NAME);
            generator.SetSerialNumber(BigInteger.ValueOf(1));
            generator.SetNotAfter(DateTime.Now.AddYears(1));
            generator.SetNotBefore(DateTime.UtcNow);
            generator.SetPublicKey(publicKey);

            return generator.Generate(factory);
        }

        private static AsymmetricCipherKeyPair GenerateEcKeyPair() {
            X9ECParameters cureParameters = SecNamedCurves.GetByName("secp256r1");
            ECDomainParameters domain = new ECDomainParameters(cureParameters.Curve, cureParameters.G, cureParameters.N);
            ECKeyGenerationParameters generationParameters = new ECKeyGenerationParameters(domain, new SecureRandom());

            ECKeyPairGenerator keyGenerator = new ECKeyPairGenerator();
            keyGenerator.Init(generationParameters);

            return keyGenerator.GenerateKeyPair();
        }
    }
}
