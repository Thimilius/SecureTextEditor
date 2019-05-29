using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.Utilities.Encoders;
using SecureTextEditor.Crypto.Digest;

namespace SecureTextEditor.Tests {
    [TestClass]
    public class DigestEngingeTester {
        private static readonly byte[] MESSAGE = Hex.Decode("000102030405060708090a0b0c0d0e0f");
        private static readonly byte[] KEY = Hex.Decode("000102030405060708090a0b0c0d0e0f");

        [TestMethod]
        public void SHA256_Test() {
            DigestEngine engine = new DigestEngine(DigestType.SHA256);
            var output = engine.Digest(MESSAGE, null);
            Assert.AreEqual(output.Length, 32);
        }

        [TestMethod]
        public void AESCMAC_Test() {
            DigestEngine engine = new DigestEngine(DigestType.AESCMAC);
            var output = engine.Digest(MESSAGE, KEY);
        }

        [TestMethod]
        public void HMACSHA256_Test() {
            DigestEngine engine = new DigestEngine(DigestType.HMACSHA256);
            var output = engine.Digest(MESSAGE, KEY);
        }
    }
}
