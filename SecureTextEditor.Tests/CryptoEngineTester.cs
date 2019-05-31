using System;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Utilities.Encoders;
using SecureTextEditor.Crypto.Cipher;

namespace SecureTextEditor.Tests {
    [TestClass]
    public class CipherEngineTester {
        private static readonly byte[] BLOCK_ALIGNED_MESSAGE   = Encoding.UTF8.GetBytes("This is my secrect text message!");
        private static readonly byte[] BLOCK_UNALIGNED_MESSAGE = Encoding.UTF8.GetBytes("This is my secrect text message");
        private static readonly byte[] MESSAGE_UNDER_ONE_BLOCK = Encoding.UTF8.GetBytes("Short message!");
        private static readonly byte[] KEY = Hex.Decode("000102030405060708090a0b0c0d0e0f");
        private static readonly byte[] IV = Hex.Decode("000102030405060708090a0b0c0d0e0f");

        private static readonly CipherPadding[] CIPHER_BLOCK_PADDINGS = (CipherPadding[])Enum.GetValues(typeof(CipherPadding));
         
        // TODO: Test weak and semi-weak keys
        // TODO: Test generation of key and iv
        // TODO: Test GCM and CCM (Check that the MAC failes after tampering)

        [TestMethod]
        public void ECB_Test() {
            foreach (var padding in CIPHER_BLOCK_PADDINGS) {
                CipherEngine engine = new CipherEngine(CipherType.Block, CipherMode.ECB, padding);

                // Use block aligned message for no padding
                byte[] message = BLOCK_UNALIGNED_MESSAGE;
                if (padding == CipherPadding.None) {
                    message = BLOCK_ALIGNED_MESSAGE;
                }

                byte[] cipher = engine.Encrypt(message, KEY, IV);
                byte[] decrypt = engine.Decrypt(cipher, KEY, IV);
                Assert.IsTrue(message.SequenceEqual(decrypt));
            }
        }

        [TestMethod]
        public void CBC_Test() {
            foreach (var padding in CIPHER_BLOCK_PADDINGS) {
                CipherEngine engine = new CipherEngine(CipherType.Block, CipherMode.CBC, padding);

                // Use block aligned message for no padding
                byte[] message = BLOCK_UNALIGNED_MESSAGE;
                if (padding == CipherPadding.None) {
                    message = BLOCK_ALIGNED_MESSAGE;
                }

                byte[] cipher = engine.Encrypt(message, KEY, IV);
                byte[] decrypt = engine.Decrypt(cipher, KEY, IV);
                Assert.IsTrue(message.SequenceEqual(decrypt));
            }
        }

        [TestMethod]
        public void CTS_Test() {
            CipherEngine engine = new CipherEngine(CipherType.Block, CipherMode.CTS, CipherPadding.None);
            byte[] cipher = engine.Encrypt(BLOCK_UNALIGNED_MESSAGE, KEY, IV);
            byte[] decrypt = engine.Decrypt(cipher, KEY, IV);
            Assert.IsTrue(BLOCK_UNALIGNED_MESSAGE.SequenceEqual(decrypt));

            // Check that CTS needs at least one block of input
            Assert.ThrowsException<DataLengthException>(() => {
                engine.Encrypt(MESSAGE_UNDER_ONE_BLOCK, KEY, IV);
            });
        }

        [TestMethod]
        public void CTR_Test() {
            CipherEngine engine = new CipherEngine(CipherType.Block, CipherMode.CTR, CipherPadding.None);
            byte[] cipher = engine.Encrypt(BLOCK_UNALIGNED_MESSAGE, KEY, IV);
            byte[] decrypt = engine.Decrypt(cipher, KEY, IV);
            Assert.IsTrue(BLOCK_UNALIGNED_MESSAGE.SequenceEqual(decrypt));
        }

        [TestMethod]
        public void CFB_Test() {
            CipherEngine engine = new CipherEngine(CipherType.Block, CipherMode.CFB, CipherPadding.None);
            byte[] cipher = engine.Encrypt(BLOCK_UNALIGNED_MESSAGE, KEY, IV);
            byte[] decrypt = engine.Decrypt(cipher, KEY, IV);
            Assert.IsTrue(BLOCK_UNALIGNED_MESSAGE.SequenceEqual(decrypt));
        }

        [TestMethod]
        public void OFB_Test() {
            CipherEngine engine = new CipherEngine(CipherType.Block, CipherMode.OFB, CipherPadding.None);
            byte[] cipher = engine.Encrypt(BLOCK_UNALIGNED_MESSAGE, KEY, IV);
            byte[] decrypt = engine.Decrypt(cipher, KEY, IV);
            Assert.IsTrue(BLOCK_UNALIGNED_MESSAGE.SequenceEqual(decrypt));
        }

        [TestMethod]
        public void Block_Mode_Out_Of_Range_Test() {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => {
                new CipherEngine(CipherType.Block, (CipherMode)999, CipherPadding.PKCS7);
            });
        }

        [TestMethod]
        public void Block_Padding_Out_Of_Range_Test() {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => {
                new CipherEngine(CipherType.Block, CipherMode.CBC, (CipherPadding)999);
            });
        }
    }
}
