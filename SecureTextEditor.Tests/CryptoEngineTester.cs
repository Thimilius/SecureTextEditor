using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Utilities.Encoders;
using SecureTextEditor.Core;

namespace SecureTextEditor.Tests {
    [TestClass]
    public class CryptoEngineTester {
        private const string BLOCK_ALIGNED_MESSAGE   = "This is my secrect text message!";
        private const string BLOCK_UNALIGNED_MESSAGE = "This is my secrect text message";
        private const string MESSAGE_UNDER_ONE_BLOCK = "Short message!";
        private static readonly byte[] KEY = Hex.Decode("000102030405060708090a0b0c0d0e0f");
        private static readonly byte[] IV = Hex.Decode("000102030405060708090a0b0c0d0e0f");

        private static readonly CipherPadding[] CIPHER_BLOCK_PADDINGS = (CipherPadding[])Enum.GetValues(typeof(CipherPadding));
         
        // TODO: Test weak and semi-weak keys
        // TODO: Test generation of key and iv

        [TestMethod]
        public void ECB_Test() {
            foreach (var padding in CIPHER_BLOCK_PADDINGS) {
                CryptoEngine engine = new CryptoEngine(CipherType.Block, CipherMode.ECB, padding, TextEncoding.UTF8);

                // Use block aligned message for no padding
                string message = BLOCK_UNALIGNED_MESSAGE;
                if (padding == CipherPadding.None) {
                    message = BLOCK_ALIGNED_MESSAGE;
                }

                byte[] cipher = engine.Encrypt(message, KEY, IV);
                string decrypt = engine.Decrypt(cipher, KEY, IV);
                Assert.AreEqual(message, decrypt);
            }
        }

        [TestMethod]
        public void CBC_Test() {
            foreach (var padding in CIPHER_BLOCK_PADDINGS) {
                CryptoEngine engine = new CryptoEngine(CipherType.Block, CipherMode.CBC, padding, TextEncoding.UTF8);

                // Use block aligned message for no padding
                string message = BLOCK_UNALIGNED_MESSAGE;
                if (padding == CipherPadding.None) {
                    message = BLOCK_ALIGNED_MESSAGE;
                }

                byte[] cipher = engine.Encrypt(message, KEY, IV);
                string decrypt = engine.Decrypt(cipher, KEY, IV);
                Assert.AreEqual(message, decrypt);
            }
        }

        [TestMethod]
        public void CTS_Test() {
            CryptoEngine engine = new CryptoEngine(CipherType.Block, CipherMode.CTS, CipherPadding.PKCS7, TextEncoding.UTF8);
            byte[] cipher = engine.Encrypt(BLOCK_UNALIGNED_MESSAGE, KEY, IV);
            string decrypt = engine.Decrypt(cipher, KEY, IV);
            Assert.AreEqual(BLOCK_UNALIGNED_MESSAGE, decrypt);

            // Check that CTS needs at least one block of input
            Assert.ThrowsException<DataLengthException>(() => {
                engine.Encrypt(MESSAGE_UNDER_ONE_BLOCK, KEY, IV);
            });
        }

        [TestMethod]
        public void CTR_Test() {
            CryptoEngine engine = new CryptoEngine(CipherType.Block, CipherMode.CTR, CipherPadding.None, TextEncoding.UTF8);
            byte[] cipher = engine.Encrypt(BLOCK_UNALIGNED_MESSAGE, KEY, IV);
            string decrypt = engine.Decrypt(cipher, KEY, IV);
            Assert.AreEqual(BLOCK_UNALIGNED_MESSAGE, decrypt);
        }

        [TestMethod]
        public void CFB_Test() {
            CryptoEngine engine = new CryptoEngine(CipherType.Block, CipherMode.CFB, CipherPadding.None, TextEncoding.UTF8);
            byte[] cipher = engine.Encrypt(BLOCK_UNALIGNED_MESSAGE, KEY, IV);
            string decrypt = engine.Decrypt(cipher, KEY, IV);
            Assert.AreEqual(BLOCK_UNALIGNED_MESSAGE, decrypt);
        }

        [TestMethod]
        public void OFB_Test() {
            CryptoEngine engine = new CryptoEngine(CipherType.Block, CipherMode.OFB, CipherPadding.None, TextEncoding.UTF8);
            byte[] cipher = engine.Encrypt(BLOCK_UNALIGNED_MESSAGE, KEY, IV);
            string decrypt = engine.Decrypt(cipher, KEY, IV);
            Assert.AreEqual(BLOCK_UNALIGNED_MESSAGE, decrypt);
        }

        [TestMethod]
        public void Block_Mode_Out_Of_Range_Test() {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => {
                new CryptoEngine(CipherType.Block, (CipherMode)999, CipherPadding.PKCS7, TextEncoding.UTF8);
            });
        }

        [TestMethod]
        public void Block_Padding_Out_Of_Range_Test() {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => {
                new CryptoEngine(CipherType.Block, CipherMode.CBC, (CipherPadding)999, TextEncoding.UTF8);
            });
        }
    }
}
