using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Modes;
using System;
using Org.BouncyCastle.Crypto.Parameters;

namespace SecureTextEditor.Core {
    public class CryptoEngine {
        private const string KEY = "000102030405060708090a0b0c0d0e0f";
        private static readonly IBlockCipher CIPHER_ENGINE = new AesEngine();

        private readonly IBufferedCipher m_Cipher;
        private readonly Encoding m_Encoding;

        public CryptoEngine(CipherMode mode, CipherPadding padding, Encoding encoding) {
            m_Cipher = GetCipherMode(mode, GetCipherPadding(padding));
            m_Encoding = encoding;
        }

        private IBlockCipherPadding GetCipherPadding(CipherPadding padding) {
            switch (padding) {
                case CipherPadding.ISO7816d4: return new ISO7816d4Padding();
                case CipherPadding.ISO10126d2: return new ISO10126d2Padding();
                case CipherPadding.PKCS5: return new Pkcs7Padding(); 
                case CipherPadding.PKCS7: return new Pkcs7Padding();
                case CipherPadding.TCB: return new TbcPadding();
                case CipherPadding.X923: return new X923Padding();
                case CipherPadding.ZeroBytes: return new ZeroBytePadding();
                default: throw new Exception();
            }
        }

        private IBufferedCipher GetCipherMode(CipherMode mode, IBlockCipherPadding padding) {
            switch (mode) {
                case CipherMode.ECB: return new PaddedBufferedBlockCipher(CIPHER_ENGINE, padding);
                case CipherMode.CBC: return new PaddedBufferedBlockCipher(CIPHER_ENGINE, padding);
                case CipherMode.CTS: return new CtsBlockCipher(CIPHER_ENGINE);
                default: throw new Exception();
            }
        }

        public string Encrypt(string message) {
            byte[] result = EncryptDecrypt(true, m_Encoding.GetBytes(message), KEY);
            return Convert.ToBase64String(result);
        }

        public string Decrypt(string cipher) {
            byte[] result = EncryptDecrypt(false, Convert.FromBase64String(cipher), KEY);
            return m_Encoding.GetString(result);
        }

        private byte[] EncryptDecrypt(bool encrypt, byte[] input, string key) {
            m_Cipher.Init(encrypt, new KeyParameter(m_Encoding.GetBytes(key)));
            return m_Cipher.DoFinal(input);
        }
    }
}
