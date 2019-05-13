using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureTextEditor.Core;

namespace SecureTextEditor.Tests {
    [TestClass]
    public class CryptoEngineTester {
        [TestMethod]
        public void ECB_Test() {
            foreach (var padding in (CipherBlockPadding[])Enum.GetValues(typeof(CipherBlockPadding))) {
                CryptoEngine engine = new CryptoEngine(CipherBlockMode.ECB, padding, Encoding.UTF8);
                string message = "This is my secrect text message!";
                string cipher = engine.Encrypt(message);
                string decrypt = engine.Decrypt(cipher);
                Assert.AreEqual(message, decrypt);
            }
        }

        [TestMethod]
        public void CBC_Test() {
            foreach (var padding in (CipherBlockPadding[])Enum.GetValues(typeof(CipherBlockPadding))) {
                CryptoEngine engine = new CryptoEngine(CipherBlockMode.CBC, padding, Encoding.UTF8);
                string message = "This is my secrect text message!";
                string cipher = engine.Encrypt(message);
                string decrypt = engine.Decrypt(cipher);
                Assert.AreEqual(message, decrypt);
            }
        }

        [TestMethod]
        public void CTS_Test() {
            CryptoEngine engine = new CryptoEngine(CipherBlockMode.CTS, CipherBlockPadding.PKCS7, Encoding.UTF8);
            string message = "This is my secrect text message!";
            string cipher = engine.Encrypt(message);
            string decrypt = engine.Decrypt(cipher);
            Assert.AreEqual(message, decrypt);
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
