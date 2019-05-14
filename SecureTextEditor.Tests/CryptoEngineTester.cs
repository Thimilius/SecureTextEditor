using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.Crypto;
using SecureTextEditor.Core;

namespace SecureTextEditor.Tests {
    [TestClass]
    public class CryptoEngineTester {
        private const string BLOCK_ALIGNED_MESSAGE   = "This is my secrect text message!";
        private const string BLOCK_UNALIGNED_MESSAGE = "This is my secrect text message";
        private const string MESSAGE_UNDER_ONE_BLOCK = "Short message!";

        private static readonly CipherBlockPadding[] CIPHER_BLOCK_PADDINGS = (CipherBlockPadding[])Enum.GetValues(typeof(CipherBlockPadding));
         
        // TODO: Test weak and semi-weak keys

        [TestMethod]
        public void ECB_Test() {
            foreach (var padding in CIPHER_BLOCK_PADDINGS) {
                CryptoEngine engine = new CryptoEngine(CipherBlockMode.ECB, padding, Encoding.UTF8);

                // Use block aligned message for no padding
                string message = BLOCK_UNALIGNED_MESSAGE;
                if (padding == CipherBlockPadding.None) {
                    message = BLOCK_ALIGNED_MESSAGE;
                }

                string cipher = engine.Encrypt(message);
                string decrypt = engine.Decrypt(cipher);
                Assert.AreEqual(message, decrypt);
            }
        }

        [TestMethod]
        public void CBC_Test() {
            foreach (var padding in CIPHER_BLOCK_PADDINGS) {
                CryptoEngine engine = new CryptoEngine(CipherBlockMode.CBC, padding, Encoding.UTF8);

                // Use block aligned message for no padding
                string message = BLOCK_UNALIGNED_MESSAGE;
                if (padding == CipherBlockPadding.None) {
                    message = BLOCK_ALIGNED_MESSAGE;
                }

                string cipher = engine.Encrypt(message);
                string decrypt = engine.Decrypt(cipher);
                Assert.AreEqual(message, decrypt);
            }
        }

        [TestMethod]
        public void CTS_Test() {
            CryptoEngine engine = new CryptoEngine(CipherBlockMode.CTS, CipherBlockPadding.PKCS7, Encoding.UTF8);
            string cipher = engine.Encrypt(BLOCK_UNALIGNED_MESSAGE);
            string decrypt = engine.Decrypt(cipher);
            Assert.AreEqual(BLOCK_UNALIGNED_MESSAGE, decrypt);

            // Check that CTS needs at least one block of input
            Assert.ThrowsException<DataLengthException>(() => {
                engine.Encrypt(MESSAGE_UNDER_ONE_BLOCK);
            });
        }

        [TestMethod]
        public void CTR_Test() {
            CryptoEngine engine = new CryptoEngine(CipherBlockMode.CTR, CipherBlockPadding.None, Encoding.UTF8);
            string cipher = engine.Encrypt(BLOCK_UNALIGNED_MESSAGE);
            string decrypt = engine.Decrypt(cipher);
            Assert.AreEqual(BLOCK_UNALIGNED_MESSAGE, decrypt);
        }

        [TestMethod]
        public void CFB_Test() {
            CryptoEngine engine = new CryptoEngine(CipherBlockMode.CFB, CipherBlockPadding.None, Encoding.UTF8);
            string cipher = engine.Encrypt(BLOCK_UNALIGNED_MESSAGE);
            string decrypt = engine.Decrypt(cipher);
            Assert.AreEqual(BLOCK_UNALIGNED_MESSAGE, decrypt);
        }

        [TestMethod]
        public void OFB_Test() {
            CryptoEngine engine = new CryptoEngine(CipherBlockMode.OFB, CipherBlockPadding.None, Encoding.UTF8);
            string cipher = engine.Encrypt(BLOCK_UNALIGNED_MESSAGE);
            string decrypt = engine.Decrypt(cipher);
            Assert.AreEqual(BLOCK_UNALIGNED_MESSAGE, decrypt);
        }

        [TestMethod]
        public void Block_Mode_Out_Of_Range_Test() {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => {
                new CryptoEngine((CipherBlockMode)999, CipherBlockPadding.PKCS7, Encoding.UTF8);
            });
        }

        [TestMethod]
        public void Block_Padding_Out_Of_Range_Test() {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => {
                new CryptoEngine(CipherBlockMode.CBC, (CipherBlockPadding)999, Encoding.UTF8);
            });
        }
    }
}
