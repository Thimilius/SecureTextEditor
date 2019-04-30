using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;

namespace SecureTextEditor.Core {
    public class CryptoPlaceholder {
        private Encoding m_Encoding;
        private IBlockCipher m_BlockCipher;
        private string m_Key;

        public CryptoPlaceholder(Encoding encoding) {
            m_Encoding = encoding;
            m_BlockCipher = new AesEngine();
            m_Key = "000102030405060708090a0b0c0d0e0f";
        }

        public string Encrypt(string message) {
            byte[] result = EncryptDecrypt(true, m_Encoding.GetBytes(message), m_Key);
            return Convert.ToBase64String(result);
        }

        public string Decrypt(string cipher) {
            byte[] result = EncryptDecrypt(false, Convert.FromBase64String(cipher), m_Key);
            return m_Encoding.GetString(result);
        }

        private byte[] EncryptDecrypt(bool encrypt, byte[] input, string key) {
            IBufferedCipher cipher = new PaddedBufferedBlockCipher(m_BlockCipher);
            cipher.Init(encrypt, new KeyParameter(m_Encoding.GetBytes(key)));
            return cipher.DoFinal(input);
        }
    }
}
