using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace SecureTextEditor.Core {
    public class CryptoEngine {
        // TODO: Generate iv instead of being it hardcoded

        private const string IV  = "0001020304050607";
        private const int STREAM_BLOCK_SIZE = 16;
        private static readonly IBlockCipher BLOCK_CIPHER_ENGINE = new AesEngine();
        private static readonly IStreamCipher STREAM_CIPHER_ENGINE = new RC4Engine();
        private static readonly int[] ACCEPTED_KEY_SIZES = new int[] { 128, 192, 256 };

        private readonly CipherType m_Type;
        private readonly IBufferedCipher m_Cipher;
        private readonly CipherBlockMode m_CipherBlockMode;
        private readonly Encoding m_Encoding;

        public CryptoEngine(CipherType type, CipherBlockMode mode, CipherBlockPadding padding, TextEncoding encoding) {
            m_Type = type;
            m_CipherBlockMode = mode;
            m_Cipher = GetCipherMode(type, mode, GetCipherPadding(padding));
            m_Encoding = GetEncoding(encoding);
        }

        /// <summary>
        /// Encrypts a plain message with the initilaized mode and returns the result encoded in Base64.
        /// </summary>
        /// <param name="message">The message to encrypt</param>
        /// <returns>The encrypted cipher</returns>
        public byte[] Encrypt(string message, byte[] key) {
            byte[] iv = null;
            if (m_Type != CipherType.Stream && m_CipherBlockMode != CipherBlockMode.ECB) {
                iv = m_Encoding.GetBytes(IV);
            }

            ICipherParameters parameters = GetCipherParameters(key, iv);
            m_Cipher.Init(true, parameters);

            byte[] result = m_Cipher.DoFinal(m_Encoding.GetBytes(message));

            return result;
        }

        /// <summary>
        /// Decrypts a given cipher encoded in Base64 an returns the plain message.
        /// </summary>
        /// <param name="cipher">The encrypted cipher</param>
        /// <returns>The plain message</returns>
        public string Decrypt(byte[] cipher, byte[] key) {
            byte[] iv = null;
            if (m_Type != CipherType.Stream && m_CipherBlockMode != CipherBlockMode.ECB) {
                iv = m_Encoding.GetBytes(IV);
            }

            ICipherParameters parameters = GetCipherParameters(key, iv);
            m_Cipher.Init(false, parameters);

            byte[] result = new byte[m_Cipher.GetOutputSize(cipher.Length)];
            int length = m_Cipher.ProcessBytes(cipher, 0, cipher.Length, result, 0);
            length += m_Cipher.DoFinal(result, length);

            return m_Encoding.GetString(result.Take(length).ToArray());
        }

        /// <summary>
        /// Generates a key for use with this engine.
        /// </summary>
        /// <param name="keySize">The size of the key</param>
        /// <returns>The generated key</returns>
        public byte[] GenerateKey(int keySize) {
            if (!ACCEPTED_KEY_SIZES.Contains(keySize)) {
                throw new ArgumentException("Invalid key size", nameof(keySize));
            }

            CipherKeyGenerator generator = new CipherKeyGenerator();
            generator.Init(new KeyGenerationParameters(new SecureRandom(), keySize));
            return generator.GenerateKey();
        }

        private IBufferedCipher GetCipherMode(CipherType type, CipherBlockMode mode, IBlockCipherPadding padding) {
            if (type == CipherType.Block) {
                switch (mode) {
                    case CipherBlockMode.ECB: return padding == null ? new BufferedBlockCipher(BLOCK_CIPHER_ENGINE) : new PaddedBufferedBlockCipher(BLOCK_CIPHER_ENGINE, padding);
                    case CipherBlockMode.CBC: return padding == null ? new BufferedBlockCipher(new CbcBlockCipher(BLOCK_CIPHER_ENGINE)) : new PaddedBufferedBlockCipher(new CbcBlockCipher(BLOCK_CIPHER_ENGINE), padding);
                    case CipherBlockMode.CTS: return new CtsBlockCipher(new CbcBlockCipher(BLOCK_CIPHER_ENGINE));
                    case CipherBlockMode.CTR: return new BufferedBlockCipher(new SicBlockCipher(BLOCK_CIPHER_ENGINE));
                    case CipherBlockMode.CFB: return new BufferedBlockCipher(new CfbBlockCipher(BLOCK_CIPHER_ENGINE, STREAM_BLOCK_SIZE));
                    case CipherBlockMode.OFB: return new BufferedBlockCipher(new OfbBlockCipher(BLOCK_CIPHER_ENGINE, STREAM_BLOCK_SIZE));
                    default: throw new ArgumentOutOfRangeException(nameof(mode));
                }
            } else {
                return new BufferedStreamCipher(STREAM_CIPHER_ENGINE);
            }
        }

        private IBlockCipherPadding GetCipherPadding(CipherBlockPadding padding) {
            switch (padding) {
                case CipherBlockPadding.None: return null;
                case CipherBlockPadding.ISO7816d4: return new ISO7816d4Padding();
                case CipherBlockPadding.ISO10126d2: return new ISO10126d2Padding();
                case CipherBlockPadding.PKCS5: return new Pkcs7Padding();
                case CipherBlockPadding.PKCS7: return new Pkcs7Padding();
                case CipherBlockPadding.TCB: return new TbcPadding();
                case CipherBlockPadding.X923: return new X923Padding();
                case CipherBlockPadding.ZeroBytes: return new ZeroBytePadding();
                default: throw new ArgumentOutOfRangeException(nameof(padding));
            }
        }

        private Encoding GetEncoding(TextEncoding encoding) {
            switch (encoding) {
                case TextEncoding.ASCII: return Encoding.ASCII;
                case TextEncoding.UTF8: return Encoding.UTF8;
                default: throw new ArgumentOutOfRangeException(nameof(encoding));
            }
        }

        private ICipherParameters GetCipherParameters(byte[] key, byte[] iv) {
            ICipherParameters result = new KeyParameter(key);
            if (iv != null) {
                result = new ParametersWithIV(result, iv);
            }
            return result;
        }
    }
}
