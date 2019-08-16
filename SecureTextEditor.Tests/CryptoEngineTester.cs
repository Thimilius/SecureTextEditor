﻿using System;
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
        private static readonly char[] PASSWORD = "Password".ToCharArray();

        private static readonly CipherPadding[] CIPHER_BLOCK_PADDINGS = (CipherPadding[])Enum.GetValues(typeof(CipherPadding));
         
        // TODO: Test weak and semi-weak keys
        // TODO: Test generation of key and iv
        // TODO: Test GCM and CCM (Check that the MAC failes after tampering)

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
                Assert.IsTrue(message.SequenceEqual(result.Result));
            }
        }

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
                Assert.IsTrue(message.SequenceEqual(result.Result));
            }
        }

        [TestMethod]
        public void CTS_Test() {
            CipherEngine engine = new CipherEngine(CipherType.AES, CipherMode.CTS, CipherPadding.None, CipherKeyOption.Generate, 128);
            byte[] cipher = engine.Encrypt(BLOCK_UNALIGNED_MESSAGE, KEY, IV);
            CipherDecryptResult result = engine.Decrypt(cipher, KEY, IV);
            Assert.IsTrue(BLOCK_UNALIGNED_MESSAGE.SequenceEqual(result.Result));

            // Check that CTS needs at least one block of input
            Assert.ThrowsException<DataLengthException>(() => {
                engine.Encrypt(MESSAGE_UNDER_ONE_BLOCK, KEY, IV);
            });
        }

        [TestMethod]
        public void CTR_Test() {
            CipherEngine engine = new CipherEngine(CipherType.AES, CipherMode.CTR, CipherPadding.None, CipherKeyOption.Generate, 128);
            byte[] cipher = engine.Encrypt(BLOCK_UNALIGNED_MESSAGE, KEY, IV);
            CipherDecryptResult result = engine.Decrypt(cipher, KEY, IV);
            Assert.IsTrue(BLOCK_UNALIGNED_MESSAGE.SequenceEqual(result.Result));
        }

        [TestMethod]
        public void CFB_Test() {
            CipherEngine engine = new CipherEngine(CipherType.AES, CipherMode.CFB, CipherPadding.None, CipherKeyOption.Generate, 128);
            byte[] cipher = engine.Encrypt(BLOCK_UNALIGNED_MESSAGE, KEY, IV);
            CipherDecryptResult result = engine.Decrypt(cipher, KEY, IV);
            Assert.IsTrue(BLOCK_UNALIGNED_MESSAGE.SequenceEqual(result.Result));
        }

        [TestMethod]
        public void OFB_Test() {
            CipherEngine engine = new CipherEngine(CipherType.AES, CipherMode.OFB, CipherPadding.None, CipherKeyOption.Generate, 128);
            byte[] cipher = engine.Encrypt(BLOCK_UNALIGNED_MESSAGE, KEY, IV);
            CipherDecryptResult result = engine.Decrypt(cipher, KEY, IV);
            Assert.IsTrue(BLOCK_UNALIGNED_MESSAGE.SequenceEqual(result.Result));
        }


        [TestMethod]
        public void SCRYPT_Test() {
            CipherEngine engine = new CipherEngine(CipherType.AES, CipherMode.GCM, CipherPadding.None, CipherKeyOption.PBEWithSCRYPT, 128);

            byte[] message = BLOCK_UNALIGNED_MESSAGE;
            byte[] key = engine.GenerateKey(PASSWORD, IV);

            byte[] cipher = engine.Encrypt(message, key, IV);
            CipherDecryptResult result = engine.Decrypt(cipher, key, IV);
            Assert.IsTrue(message.SequenceEqual(result.Result));
        }

        [TestMethod]
        public void PBEWithSHA256And128BitAESCBCBC_Test() {
            CipherEngine engine = new CipherEngine(CipherType.AES, CipherMode.CBC, CipherPadding.PKCS7, CipherKeyOption.PBE, 128);

            byte[] message = BLOCK_UNALIGNED_MESSAGE;
            byte[] key = engine.GenerateKey(PASSWORD, IV);

            byte[] cipher = engine.Encrypt(message, key, IV);
            CipherDecryptResult result = engine.Decrypt(cipher, key, IV);
            Assert.IsTrue(message.SequenceEqual(result.Result));
        }

        [TestMethod]
        public void PBEWithSHAAnd40BitRC4_Test() {
            CipherEngine engine = new CipherEngine(CipherType.RC4, CipherMode.None, CipherPadding.None, CipherKeyOption.PBE, 40);

            byte[] message = BLOCK_UNALIGNED_MESSAGE;
            byte[] key = engine.GenerateKey(PASSWORD, IV);

            byte[] cipher = engine.Encrypt(message, key, IV);
            CipherDecryptResult result = engine.Decrypt(cipher, key, IV);
            Assert.IsTrue(message.SequenceEqual(result.Result));
        }

        [TestMethod]
        public void Block_Mode_Out_Of_Range_Test() {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => {
                new CipherEngine(CipherType.AES, (CipherMode)999, CipherPadding.PKCS7, CipherKeyOption.Generate, 128);
            });
        }

        [TestMethod]
        public void Block_Padding_Out_Of_Range_Test() {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => {
                new CipherEngine(CipherType.AES, CipherMode.CBC, (CipherPadding)999, CipherKeyOption.Generate, 128);
            });
        }
    }
}
