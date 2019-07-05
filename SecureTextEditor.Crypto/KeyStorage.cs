using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Operators;
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
    public class KeyStorage {
        private const string KEY_STORE_ALIAS = "key";
        private static readonly X509Name CERTIFICATE_NAME = new X509Name("CN=KeyCA");

        private Pkcs12Store m_Store;

        public KeyStorage() {
            m_Store = new Pkcs12Store();
        }

        public void Store(SignatureKeyPair pair) {
            X509Certificate certificate = GetCertificate(pair.Pair.Private, pair.Pair.Public);
            m_Store.Load(null, null);
            X509CertificateEntry[] certificates = new X509CertificateEntry[] { new X509CertificateEntry(certificate) };
            AsymmetricKeyEntry entry = new AsymmetricKeyEntry(pair.Pair.Private);
            m_Store.SetKeyEntry(KEY_STORE_ALIAS, entry, certificates);

            using (FileStream stream = new FileStream("storage.fks", FileMode.Create)) {
                m_Store.Save(stream, "password".ToCharArray(), new SecureRandom());
            }
        }

        public SignatureKeyPair Retrieve() {
            return null;
        }

        private X509Certificate GetCertificate(AsymmetricKeyParameter privateKey, AsymmetricKeyParameter publicKey) {
            // TODO: We need to self sign this certificate
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
    }
}
