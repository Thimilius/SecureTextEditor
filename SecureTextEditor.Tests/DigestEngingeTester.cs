using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.Utilities.Encoders;
using SecureTextEditor.Crypto;
using SecureTextEditor.Crypto.Digest;

namespace SecureTextEditor.Tests {
    [TestClass]
    public class DigestEngingeTester {
        private static readonly byte[] MESSAGE = Hex.Decode("000102030405060708090a0b0c0d0e0f");
        private static readonly byte[] ALTERED_MESSAGE = Hex.Decode("000102030405060708090a0b0c0d0e00");
        private static readonly byte[] KEY = Hex.Decode("000102030405060708090a0b0c0d0e0f");
        private static readonly byte[] ALTERED_KEY = Hex.Decode("000102030405060708090a0b0c0d0e00");

        [TestMethod]
        public void SHA256_Test() {
            DigestEngine engine = new DigestEngine(DigestType.SHA256);
            byte[] digest1 = engine.Digest(MESSAGE, null);
            byte[] digest2 = engine.Digest(ALTERED_MESSAGE, null);
            Assert.IsFalse(SecurityExtensions.AreEqual(digest1, digest2));
        }

        [TestMethod]
        public void AESCMAC_Test() {
            DigestEngine engine = new DigestEngine(DigestType.AESCMAC);
            byte[] digest1 = engine.Digest(MESSAGE, KEY);
            byte[] digest2 = engine.Digest(ALTERED_MESSAGE, KEY);
            Assert.IsFalse(SecurityExtensions.AreEqual(digest1, digest2));
        }

        [TestMethod]
        public void HMACSHA256_Test() {
            DigestEngine engine = new DigestEngine(DigestType.HMACSHA256);
            byte[] digest1 = engine.Digest(MESSAGE, KEY);
            byte[] digest2 = engine.Digest(ALTERED_MESSAGE, KEY);
            Assert.IsFalse(SecurityExtensions.AreEqual(digest1, digest2));
        }

        [TestMethod]
        public void Key_Test() {
            DigestEngine engine = new DigestEngine(DigestType.HMACSHA256);
            byte[] digest1 = engine.Digest(MESSAGE, KEY);
            byte[] digest2 = engine.Digest(MESSAGE, ALTERED_KEY);
            Assert.IsFalse(SecurityExtensions.AreEqual(digest1, digest2));
        }
    }
}
