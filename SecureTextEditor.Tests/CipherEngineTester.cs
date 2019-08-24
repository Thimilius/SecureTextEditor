using System;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Utilities.Encoders;
using SecureTextEditor.Crypto.Cipher;

namespace SecureTextEditor.Tests {
    /// <summary>
    /// Tester for the cipher engine.
    /// </summary>
    [TestClass]
    public class CipherEngineTester {
        /// <summary>
        /// A block aligned message encoded in UTF8.
        /// </summary>
        private static readonly byte[] BLOCK_ALIGNED_MESSAGE   = Encoding.UTF8.GetBytes("This is my secrect text message!");
        /// <summary>
        /// A block unaligned message encoded in UTF8.
        /// </summary>
        private static readonly byte[] BLOCK_UNALIGNED_MESSAGE = Encoding.UTF8.GetBytes("This is my secrect text message");
        /// <summary>
        /// A block unaligned mesasge under one block encoded in UTF8.
        /// </summary>
        private static readonly byte[] MESSAGE_UNDER_ONE_BLOCK = Encoding.UTF8.GetBytes("Short message!");
        /// <summary>
        /// A fixed symmetric key.
        /// </summary>
        private static readonly byte[] KEY = Hex.Decode("000102030405060708090a0b0c0d0e0f");
        /// <summary>
        /// A fixed initialization vector.
        /// </summary>
        private static readonly byte[] IV = Hex.Decode("000102030405060708090a0b0c0d0e0f");
        /// <summary>
        /// A fixed password for PBE.
        /// </summary>
        private static readonly char[] PASSWORD = "Password".ToCharArray();
        /// <summary>
        /// A list of all available cipher paddings.
        /// </summary>
        private static readonly CipherPadding[] CIPHER_BLOCK_PADDINGS = (CipherPadding[])Enum.GetValues(typeof(CipherPadding));
         
        /// <summary>
        /// Tests AES ECB with all paddings.
        /// </summary>
        [TestMethod]
        public void ECB_Test() {
            foreach (var padding in CIPHER_BLOCK_PADDINGS) {
                CipherEngine engine = new CipherEngine(CipherType.AES, CipherMode.ECB, padding, CipherKeyOption.Generate, 128);

                // Use block aligned message for no padding
                byte[] message = BLOCK_UNALIGNED_MESSAGE;
                if (padding == CipherPadding.None) {
                    message = BLOCK_ALIGNED_MESSAGE;
                }

                byte[] cipher = engine.Encrypt(message, KEY, IV);
                CipherDecryptResult result = engine.Decrypt(cipher, KEY, IV);

                Assert.IsTrue(result.Status == CipherDecryptStatus.Success);
                Assert.IsTrue(message.SequenceEqual(result.Result));
            }
        }

        /// <summary>
        /// Tests AES CBC with all padddings.
        /// </summary>
        [TestMethod]
        public void CBC_Test() {
            foreach (var padding in CIPHER_BLOCK_PADDINGS) {
                CipherEngine engine = new CipherEngine(CipherType.AES, CipherMode.CBC, padding, CipherKeyOption.Generate, 128);

                // Use block aligned message for no padding
                byte[] message = BLOCK_UNALIGNED_MESSAGE;
                if (padding == CipherPadding.None) {
                    message = BLOCK_ALIGNED_MESSAGE;
                }

                byte[] cipher = engine.Encrypt(message, KEY, IV);
                CipherDecryptResult result = engine.Decrypt(cipher, KEY, IV);

                Assert.IsTrue(result.Status == CipherDecryptStatus.Success);
                Assert.IsTrue(message.SequenceEqual(result.Result));
            }
        }

        /// <summary>
        /// Tests AES CTS.
        /// </summary>
        [TestMethod]
        public void CTS_Test() {
            CipherEngine engine = new CipherEngine(CipherType.AES, CipherMode.CTS, CipherPadding.None, CipherKeyOption.Generate, 128);
            byte[] cipher = engine.Encrypt(BLOCK_UNALIGNED_MESSAGE, KEY, IV);
            CipherDecryptResult result = engine.Decrypt(cipher, KEY, IV);

            Assert.IsTrue(result.Status == CipherDecryptStatus.Success);
            Assert.IsTrue(BLOCK_UNALIGNED_MESSAGE.SequenceEqual(result.Result));

            // Check that CTS needs at least one block of input
            Assert.ThrowsException<DataLengthException>(() => {
                engine.Encrypt(MESSAGE_UNDER_ONE_BLOCK, KEY, IV);
            });
        }

        /// <summary>
        /// Tests AES CTR.
        /// </summary>
        [TestMethod]
        public void CTR_Test() {
            CipherEngine engine = new CipherEngine(CipherType.AES, CipherMode.CTR, CipherPadding.None, CipherKeyOption.Generate, 128);
            byte[] cipher = engine.Encrypt(BLOCK_UNALIGNED_MESSAGE, KEY, IV);
            CipherDecryptResult result = engine.Decrypt(cipher, KEY, IV);

            Assert.IsTrue(result.Status == CipherDecryptStatus.Success);
            Assert.IsTrue(BLOCK_UNALIGNED_MESSAGE.SequenceEqual(result.Result));
        }

        /// <summary>
        /// Tests AES CFB.
        /// </summary>
        [TestMethod]
        public void CFB_Test() {
            CipherEngine engine = new CipherEngine(CipherType.AES, CipherMode.CFB, CipherPadding.None, CipherKeyOption.Generate, 128);
            byte[] cipher = engine.Encrypt(BLOCK_UNALIGNED_MESSAGE, KEY, IV);
            CipherDecryptResult result = engine.Decrypt(cipher, KEY, IV);

            Assert.IsTrue(result.Status == CipherDecryptStatus.Success);
            Assert.IsTrue(BLOCK_UNALIGNED_MESSAGE.SequenceEqual(result.Result));
        }

        /// <summary>
        /// Tests AES OFB.
        /// </summary>
        [TestMethod]
        public void OFB_Test() {
            CipherEngine engine = new CipherEngine(CipherType.AES, CipherMode.OFB, CipherPadding.None, CipherKeyOption.Generate, 128);
            byte[] cipher = engine.Encrypt(BLOCK_UNALIGNED_MESSAGE, KEY, IV);
            CipherDecryptResult result = engine.Decrypt(cipher, KEY, IV);

            Assert.IsTrue(result.Status == CipherDecryptStatus.Success);
            Assert.IsTrue(BLOCK_UNALIGNED_MESSAGE.SequenceEqual(result.Result));
        }

        /// <summary>
        /// Tests AES GCM.
        /// </summary>
        [TestMethod]
        public void GCM_Test() {
            CipherEngine engine = new CipherEngine(CipherType.AES, CipherMode.GCM, CipherPadding.None, CipherKeyOption.Generate, 128);
            byte[] cipher = engine.Encrypt(BLOCK_UNALIGNED_MESSAGE, KEY, IV);
            CipherDecryptResult result = engine.Decrypt(cipher, KEY, IV);

            Assert.IsTrue(result.Status == CipherDecryptStatus.Success);
            Assert.IsTrue(BLOCK_UNALIGNED_MESSAGE.SequenceEqual(result.Result));

            // Test MAC failure
            cipher[0] = (byte)~cipher[0];
            result = engine.Decrypt(cipher, KEY, IV);

            Assert.IsTrue(result.Status == CipherDecryptStatus.MacFailed);
        }

        /// <summary>
        /// Tests AES CCM.
        /// </summary>
        [TestMethod]
        public void CCM_Test() {
            CipherEngine engine = new CipherEngine(CipherType.AES, CipherMode.CCM, CipherPadding.None, CipherKeyOption.Generate, 128);
            byte[] nonce = engine.GenerateIV();
            byte[] cipher = engine.Encrypt(BLOCK_UNALIGNED_MESSAGE, KEY, nonce);
            CipherDecryptResult result = engine.Decrypt(cipher, KEY, nonce);

            Assert.IsTrue(result.Status == CipherDecryptStatus.Success);
            Assert.IsTrue(BLOCK_UNALIGNED_MESSAGE.SequenceEqual(result.Result));
        }

        /// <summary>
        /// Tests AES PBE.
        /// </summary>
        [TestMethod]
        public void AESPBE_Test() {
            CipherEngine engine = new CipherEngine(CipherType.AES, CipherMode.CBC, CipherPadding.PKCS7, CipherKeyOption.PBE, 128);

            byte[] message = BLOCK_UNALIGNED_MESSAGE;
            byte[] key = engine.GenerateKey(PASSWORD, IV);

            byte[] cipher = engine.Encrypt(message, key, IV);
            CipherDecryptResult result = engine.Decrypt(cipher, key, IV);

            Assert.IsTrue(result.Status == CipherDecryptStatus.Success);
            Assert.IsTrue(message.SequenceEqual(result.Result));
        }

        /// <summary>
        /// Tests RC4 PBE.
        /// </summary>
        [TestMethod]
        public void RC4PBE_Test() {
            CipherEngine engine = new CipherEngine(CipherType.RC4, CipherMode.None, CipherPadding.None, CipherKeyOption.PBE, 40);

            byte[] message = BLOCK_UNALIGNED_MESSAGE;
            byte[] key = engine.GenerateKey(PASSWORD, IV);

            byte[] cipher = engine.Encrypt(message, key, IV);
            CipherDecryptResult result = engine.Decrypt(cipher, key, IV);

            Assert.IsTrue(result.Status == CipherDecryptStatus.Success);
            Assert.IsTrue(message.SequenceEqual(result.Result));
        }

        /// <summary>
        /// Tests AES PBEWithSCRYPT.
        /// </summary>
        [TestMethod]
        public void SCRYPT_Test() {
            CipherEngine engine = new CipherEngine(CipherType.AES, CipherMode.GCM, CipherPadding.None, CipherKeyOption.PBEWithSCRYPT, 128);

            byte[] message = BLOCK_UNALIGNED_MESSAGE;
            byte[] key = engine.GenerateKey(PASSWORD, IV);

            byte[] cipher = engine.Encrypt(message, key, IV);
            CipherDecryptResult result = engine.Decrypt(cipher, key, IV);

            Assert.IsTrue(result.Status == CipherDecryptStatus.Success);
            Assert.IsTrue(message.SequenceEqual(result.Result));
        }

        /// <summary>
        /// Tests invalid parameters of digest engine.
        /// </summary>
        [TestMethod]
        public void InvalidParameters_Test() {
            // Test key sizes
            Assert.ThrowsException<InvalidOperationException>(() => new CipherEngine(CipherType.AES, CipherMode.CBC, CipherPadding.PKCS7, CipherKeyOption.Generate, 18273));
            Assert.ThrowsException<InvalidOperationException>(() => new CipherEngine(CipherType.RC4, CipherMode.None, CipherPadding.None, CipherKeyOption.Generate, 18273));

            // Test PBE parameters
            Assert.ThrowsException<InvalidOperationException>(() => new CipherEngine(CipherType.AES, CipherMode.CTS, CipherPadding.None, CipherKeyOption.PBE, 256));
            Assert.ThrowsException<InvalidOperationException>(() => new CipherEngine(CipherType.AES, CipherMode.CTS, CipherPadding.None, CipherKeyOption.PBEWithSCRYPT, 256));

            // Test wrong enum
            Assert.ThrowsException<InvalidOperationException>(() => new CipherEngine(CipherType.AES, (CipherMode)999, CipherPadding.PKCS7, CipherKeyOption.Generate, 128));
            Assert.ThrowsException<InvalidOperationException>(() => new CipherEngine(CipherType.AES, CipherMode.CBC, (CipherPadding)999, CipherKeyOption.Generate, 128));
        }
    }
}
