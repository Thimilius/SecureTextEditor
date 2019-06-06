using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.Utilities.Encoders;
using SecureTextEditor.Crypto;
using SecureTextEditor.Crypto.Cipher;

namespace SecureTextEditor.Tests {
    [TestClass]
    public class PBETester {
        private static readonly byte[] BLOCK_ALIGNED_MESSAGE = Encoding.UTF8.GetBytes("This is my secrect text message!");
        private static readonly byte[] BLOCK_UNALIGNED_MESSAGE = Encoding.UTF8.GetBytes("This is my secrect text message");
        private static readonly byte[] MESSAGE_UNDER_ONE_BLOCK = Encoding.UTF8.GetBytes("Short message!");
        private static readonly byte[] IV = Hex.Decode("000102030405060708090a0b0c0d0e0f");

        [TestMethod]
        public void SCRYPT_Test() {
            CipherEngine engine = new CipherEngine(CipherType.Block, CipherMode.GCM, CipherPadding.None, KeyType.PBEWithSCRYPT, 128);

            byte[] message = BLOCK_UNALIGNED_MESSAGE;
            char[] password = "Password".ToCharArray();
            byte[] key = engine.GenerateKey(password);

            byte[] cipher = engine.Encrypt(message, key, IV);
            CipherDecryptResult result = engine.Decrypt(cipher, key, IV);
            Assert.IsTrue(message.SequenceEqual(result.Result));
        }
    }
}
