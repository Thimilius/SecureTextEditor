using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureTextEditor.Crypto;
using SecureTextEditor.Crypto.Signature;

namespace SecureTextEditor.Tests {
    /// <summary>
    /// Tester for the siganture engine.
    /// </summary>
    [TestClass]
    public class SignatureEngineTester {
        private static readonly byte[] BLOCK_UNALIGNED_MESSAGE = Encoding.UTF8.GetBytes("This is my secrect text message");

        /// <summary>
        /// Test signature via DSAWITHSHA256.
        /// </summary>
        [TestMethod]
        public void DSAWithSHA256_Test() {
            foreach (var key in SignatureEngine.DSA_ACCEPTED_KEYS) {
                SignatureEngine engine = new SignatureEngine(SignatureType.DSAWithSHA256, key);
                SignatureKeyPair keyPair = engine.GenerateKeyPair();

                byte[] sign = engine.Sign(BLOCK_UNALIGNED_MESSAGE, keyPair.PrivateKey);
                bool verify = engine.Verify(BLOCK_UNALIGNED_MESSAGE, sign, keyPair.PublicKey);

                Assert.IsTrue(verify);

                // Test that we do not get the same sign
                byte[] secondSign = engine.Sign(BLOCK_UNALIGNED_MESSAGE, keyPair.PrivateKey);
                Assert.IsFalse(SecurityExtensions.AreEqual(sign, secondSign));
            }
        }

        /// <summary>
        /// Test signature via ECDSAWithSHA256.
        /// </summary>
        [TestMethod]
        public void ECDSAWithSHA256_Test() {
            foreach (var key in SignatureEngine.ECDSA_ACCEPTED_KEYS) {
                SignatureEngine engine = new SignatureEngine(SignatureType.ECDSAWithSHA256, key);
                SignatureKeyPair keyPair = engine.GenerateKeyPair();

                byte[] sign = engine.Sign(BLOCK_UNALIGNED_MESSAGE, keyPair.PrivateKey);
                bool verify = engine.Verify(BLOCK_UNALIGNED_MESSAGE, sign, keyPair.PublicKey);

                Assert.IsTrue(verify);

                // Test that we do not get the same sign
                byte[] secondSign = engine.Sign(BLOCK_UNALIGNED_MESSAGE, keyPair.PrivateKey);
                Assert.IsFalse(SecurityExtensions.AreEqual(sign, secondSign));
            }
        }

        /// <summary>
        /// Test invalid parameters of signature engine.
        /// </summary>
        [TestMethod]
        public void InvalidParameters_Test() {
            // Test wrong key size
            Assert.ThrowsException<InvalidOperationException>(() => new SignatureEngine(SignatureType.DSAWithSHA256, 12738));
            Assert.ThrowsException<InvalidOperationException>(() => new SignatureEngine(SignatureType.ECDSAWithSHA256, 12738));

            // Test wrong enum
            Assert.ThrowsException<InvalidOperationException>(() => new SignatureEngine(SignatureType.None, 1024));
        }

        /// <summary>
        /// Tests that the byte arrays of the signature key pair get cleared properly.
        /// </summary>
        [TestMethod]
        public void ClearSignaturePair_Test() {
            SignatureEngine engine = new SignatureEngine(SignatureType.ECDSAWithSHA256, 256);
            SignatureKeyPair pair = engine.GenerateKeyPair();

            // Clone arrays for latter comparison
            byte[] privateKey = (byte[])pair.PrivateKey.Clone();
            byte[] publicKey = (byte[])pair.PublicKey.Clone();

            pair.Clear();

            Assert.IsFalse(SecurityExtensions.AreEqual(privateKey, pair.PrivateKey));
            Assert.IsFalse(SecurityExtensions.AreEqual(publicKey, pair.PublicKey));
        }
    }
}
