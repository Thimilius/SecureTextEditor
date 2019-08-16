using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureTextEditor.Crypto;
using SecureTextEditor.Crypto.Signature;
using SecureTextEditor.Crypto.Storage;

namespace SecureTextEditor.Tests {
    [TestClass]
    public class StorageTester {
        private const string KEY_STORAGE_PATH = "storage.fks";
        private const string KEY_STORAGE_ALIAS = "signature_private_key";
        private static readonly char[] KEY_STORAGE_PASSWORD = "password".ToCharArray();

        // TODO: Test wrong key storage password

        [TestMethod]
        public void KeyStorage_Test() {
            SignatureEngine engine = new SignatureEngine(SignatureType.DSAWithSHA256, 1024);
            SignatureKeyPair keyPair = engine.GenerateKeyPair();
            KeyStorage storage = new KeyStorage(KEY_STORAGE_PATH);
            storage.Store(KEY_STORAGE_ALIAS, keyPair);
            storage.Save(KEY_STORAGE_PASSWORD);
            storage.Load(KEY_STORAGE_PASSWORD);
            SignatureKeyPair loadedPair = storage.Retrieve(KEY_STORAGE_ALIAS);
            Assert.IsTrue(SecurityExtensions.AreEqual(keyPair.PrivateKey, loadedPair.PrivateKey));
            Assert.IsTrue(SecurityExtensions.AreEqual(keyPair.PublicKey, loadedPair.PublicKey));
        }
    }
}
