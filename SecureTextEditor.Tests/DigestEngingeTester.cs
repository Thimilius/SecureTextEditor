using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.Utilities.Encoders;
using SecureTextEditor.Crypto;
using SecureTextEditor.Crypto.Digest;
using System;

namespace SecureTextEditor.Tests {
    /// <summary>
    /// Tester for the digest engine.
    /// </summary>
    [TestClass]
    public class DigestEngingeTester {
        private static readonly byte[] MESSAGE = Hex.Decode("000102030405060708090a0b0c0d0e0f");
        private static readonly byte[] ALTERED_MESSAGE = Hex.Decode("000102030405060708090a0b0c0d0e00");
        private static readonly byte[] KEY = Hex.Decode("000102030405060708090a0b0c0d0e0f");

        /// <summary>
        /// Test the SHA256 hash.
        /// </summary>
        [TestMethod]
        public void SHA256_Test() {
            DigestEngine engine = new DigestEngine(DigestType.SHA256);
            byte[] digest1 = engine.Digest(MESSAGE, null);
            byte[] digest2 = engine.Digest(ALTERED_MESSAGE, null);
            Assert.IsFalse(SecurityExtensions.AreEqual(digest1, digest2));
        }

        /// <summary>
        /// Tests the AESCMAC MAC.
        /// </summary>
        [TestMethod]
        public void AESCMAC_Test() {
            DigestEngine engine = new DigestEngine(DigestType.AESCMAC);
            byte[] digest1 = engine.Digest(MESSAGE, KEY);
            byte[] digest2 = engine.Digest(ALTERED_MESSAGE, KEY);
            Assert.IsFalse(SecurityExtensions.AreEqual(digest1, digest2));
        }

        /// <summary>
        /// Tests the HMACSHA256 MAC.
        /// </summary>
        [TestMethod]
        public void HMACSHA256_Test() {
            DigestEngine engine = new DigestEngine(DigestType.HMACSHA256);
            byte[] digest1 = engine.Digest(MESSAGE, KEY);
            byte[] digest2 = engine.Digest(ALTERED_MESSAGE, KEY);
            Assert.IsFalse(SecurityExtensions.AreEqual(digest1, digest2));
        }

        /// <summary>
        /// Tests invalid parameters of digest engine.
        /// </summary>
        [TestMethod]
        public void InvalidParameters_Test() {
            Assert.ThrowsException<InvalidOperationException>(() => new DigestEngine(DigestType.None));
        }

        /// <summary>
        /// Tests the key generation.
        /// </summary>
        [TestMethod]
        public void Key_Test() {
            // Test hash
            DigestEngine hashEngine = new DigestEngine(DigestType.SHA256);
            Assert.IsTrue(hashEngine.GetDigestLength() == 32);
            Assert.IsNull(hashEngine.GenerateKey());

            // Test MAC
            DigestEngine macEngine = new DigestEngine(DigestType.HMACSHA256);
            Assert.IsTrue(macEngine.GetDigestLength() == 32);
            Assert.IsNotNull(macEngine.GenerateKey());
        }
    }
}
