using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureTextEditor.Crypto.Digest;
using SecureTextEditor.Crypto.Signature;
using SecureTextEditor.Crypto.Storage;

namespace SecureTextEditor.Tests {
    [TestClass]
    public class SignatureEngineTester {
        private const string KEY_STORAGE_PATH = "storage.fks";
        private const string KEY_STORAGE_ALIAS = "signature_private_key";
        private static readonly char[] KEY_STORAGE_PASSWORD = "password".ToCharArray();
        private static readonly byte[] BLOCK_UNALIGNED_MESSAGE = Encoding.UTF8.GetBytes("This is my secrect text message");

        // TODO: Test that signatures are different for non-deterministic dsa

        [TestMethod]
        public void DSAWithSHA256_Test() {
            SignatureEngine engine = new SignatureEngine(SignatureType.DSAWithSHA256, 1024);
            SignatureKeyPair keyPair = engine.GenerateKeyPair();
            byte[] sign = engine.Sign(BLOCK_UNALIGNED_MESSAGE, keyPair.PrivateKey);
            bool verify = engine.Verify(BLOCK_UNALIGNED_MESSAGE, sign, keyPair.PublicKey);
            Assert.IsTrue(verify);
        }

        [TestMethod]
        public void ECDSAWithSHA256_Test() {
            SignatureEngine engine = new SignatureEngine(SignatureType.ECDSAWithSHA256, 256);
            SignatureKeyPair keyPair = engine.GenerateKeyPair();
            byte[] sign = engine.Sign(BLOCK_UNALIGNED_MESSAGE, keyPair.PrivateKey);
            bool verify = engine.Verify(BLOCK_UNALIGNED_MESSAGE, sign, keyPair.PublicKey);
            Assert.IsTrue(verify);
        }

        [TestMethod]
        public void KeyStorage_Test() {
            SignatureEngine engine = new SignatureEngine(SignatureType.DSAWithSHA256, 1024);
            SignatureKeyPair keyPair = engine.GenerateKeyPair();
            KeyStorage storage = new KeyStorage(KEY_STORAGE_PATH);
            storage.Store(KEY_STORAGE_ALIAS, keyPair);
            storage.Save(KEY_STORAGE_PASSWORD);
            storage.Load(KEY_STORAGE_PASSWORD);
            SignatureKeyPair loadedPair = storage.Retrieve(KEY_STORAGE_ALIAS);
            Assert.IsTrue(DigestEngine.AreEqual(keyPair.PrivateKey, loadedPair.PrivateKey));
            Assert.IsTrue(DigestEngine.AreEqual(keyPair.PublicKey, loadedPair.PublicKey));
        }
    }
}
