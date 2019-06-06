using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.Utilities.Encoders;
using SecureTextEditor.Crypto.Cipher;

namespace SecureTextEditor.Tests {
    [TestClass]
    public class PBETester {
        private static readonly byte[] BLOCK_UNALIGNED_MESSAGE = Encoding.UTF8.GetBytes("This is my secrect text message");
        private static readonly byte[] IV = Hex.Decode("000102030405060708090a0b0c0d0e0f");
        private static readonly char[] PASSWORD = "Password".ToCharArray();

        [TestMethod]
        public void SCRYPT_Test() {
            CipherEngine engine = new CipherEngine(CipherType.AES, CipherMode.GCM, CipherPadding.None, CipherKeyOption.PBEWithSCRYPT, 128);

            byte[] message = BLOCK_UNALIGNED_MESSAGE;
            byte[] key = engine.GenerateKey(PASSWORD);

            byte[] cipher = engine.Encrypt(message, key, IV);
            CipherDecryptResult result = engine.Decrypt(cipher, key, IV);
            Assert.IsTrue(message.SequenceEqual(result.Result));
        }

        [TestMethod]
        public void PBEWithSHA256And128BitAESCBCBC_Test() {
            CipherEngine engine = new CipherEngine(CipherType.AES, CipherMode.CBC, CipherPadding.PKCS7, CipherKeyOption.PBE, 128);

            byte[] message = BLOCK_UNALIGNED_MESSAGE;
            byte[] key = engine.GenerateKey(PASSWORD);

            byte[] cipher = engine.Encrypt(message, key, IV);
            CipherDecryptResult result = engine.Decrypt(cipher, key, IV);
            Assert.IsTrue(message.SequenceEqual(result.Result));
        }

        [TestMethod]
        public void PBEWithSHAAnd40BitRC4_Test() {
            CipherEngine engine = new CipherEngine(CipherType.RC4, CipherMode.None, CipherPadding.None, CipherKeyOption.PBE, 40);

            byte[] message = BLOCK_UNALIGNED_MESSAGE;
            byte[] key = engine.GenerateKey(PASSWORD);

            byte[] cipher = engine.Encrypt(message, key, IV);
            CipherDecryptResult result = engine.Decrypt(cipher, key, IV);
            Assert.IsTrue(message.SequenceEqual(result.Result));
        }
    }
}
