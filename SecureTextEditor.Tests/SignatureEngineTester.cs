using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureTextEditor.Crypto.Signature;

namespace SecureTextEditor.Tests {
    [TestClass]
    public class SignatureEngineTester {
        private static readonly byte[] BLOCK_UNALIGNED_MESSAGE = Encoding.UTF8.GetBytes("This is my secrect text message");

        // TODO: Test that signatures are different for non-deterministic dsa

        [TestMethod]
        public void DSAWithSHA256_Test() {
            SignatureEngine engine = new SignatureEngine(SignatureType.DSAWithSHA256, 1024);
            SignatureKeyPair keyPair = engine.GenerateKeyPair();
            byte[] sign = engine.Sign(BLOCK_UNALIGNED_MESSAGE, keyPair.PrivateKey);
            bool verify = engine.Verify(BLOCK_UNALIGNED_MESSAGE, sign, keyPair.PublicKey);
            Assert.IsTrue(verify);
        }

        [TestMethod]
        public void ECDSAWithSHA256_Test() {
            SignatureEngine engine = new SignatureEngine(SignatureType.ECDSAWithSHA256, 256);
            SignatureKeyPair keyPair = engine.GenerateKeyPair();
            byte[] sign = engine.Sign(BLOCK_UNALIGNED_MESSAGE, keyPair.PrivateKey);
            bool verify = engine.Verify(BLOCK_UNALIGNED_MESSAGE, sign, keyPair.PublicKey);
            Assert.IsTrue(verify);
        }
    }
}
