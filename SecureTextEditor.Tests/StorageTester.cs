using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureTextEditor.Crypto;
using SecureTextEditor.Crypto.Signature;
using SecureTextEditor.Crypto.Storage;

namespace SecureTextEditor.Tests {
    /// <summary>
    /// Tester for the key storage.
    /// </summary>
    [TestClass]
    public class StorageTester {
        /// <summary>
        /// The path to the key storage.
        /// </summary>
        private const string KEY_STORAGE_PATH = "storage.fks";
        /// <summary>
        /// The alias used to store the private key in the key storage.
        /// </summary>
        private const string KEY_STORAGE_ALIAS = "signature_private_key";
        /// <summary>
        /// The password for the key storage.
        /// </summary>
        private static readonly char[] KEY_STORAGE_PASSWORD = "password".ToCharArray();
        /// <summary>
        /// A wrong password for the key storage.
        /// </summary>
        private static readonly char[] KEY_STORAGE_WRONG_PASSWORD = "wrong_password".ToCharArray();

        /// <summary>
        /// Tests simple storing and loading from key storage.
        /// </summary>
        [TestMethod]
        public void KeyStorage_Test() {
            SignatureEngine engine = new SignatureEngine(SignatureType.DSAWithSHA256, 1024);
            SignatureKeyPair keyPair = engine.GenerateKeyPair();
            KeyStorage storage = new KeyStorage(KEY_STORAGE_PATH);

            storage.Store(KEY_STORAGE_ALIAS, keyPair);
            storage.Save(KEY_STORAGE_PASSWORD);
            Assert.IsTrue(storage.Load(KEY_STORAGE_PASSWORD).Status == KeyStorageLoadStatus.Success);
            SignatureKeyPair loadedPair = storage.Retrieve(KEY_STORAGE_ALIAS);

            Assert.IsTrue(SecurityExtensions.AreEqual(keyPair.PrivateKey, loadedPair.PrivateKey));
            Assert.IsTrue(SecurityExtensions.AreEqual(keyPair.PublicKey, loadedPair.PublicKey));
        }

        /// <summary>
        /// Tests that a wrong password fails to load.
        /// </summary>
        [TestMethod]
        public void WrongPassword_Test() {
            SignatureEngine engine = new SignatureEngine(SignatureType.DSAWithSHA256, 1024);
            SignatureKeyPair keyPair = engine.GenerateKeyPair();
            KeyStorage storage = new KeyStorage(KEY_STORAGE_PATH);

            storage.Store(KEY_STORAGE_ALIAS, keyPair);
            storage.Save(KEY_STORAGE_PASSWORD);
            Assert.IsTrue(storage.Load(KEY_STORAGE_WRONG_PASSWORD).Status == KeyStorageLoadStatus.PasswordWrong);
        }
    }
}
