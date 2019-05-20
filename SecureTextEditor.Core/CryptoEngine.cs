using System;
using System.Linq;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace SecureTextEditor.Core {
    /// <summary>
    /// Cryptographic engine abstracting block (AES) and stream (RC4) ciphers.
    /// </summary>
    public class CryptoEngine {
        private const int STREAM_BLOCK_SIZE = 16;
        private static readonly IBlockCipher BLOCK_CIPHER_ENGINE = new AesEngine();
        private static readonly IStreamCipher STREAM_CIPHER_ENGINE = new RC4Engine();
        private static readonly int[] ACCEPTED_KEY_SIZES = new int[] { 128, 192, 256 };

        private readonly CipherType m_Type;
        private readonly CipherMode m_CipherBlockMode;
        private readonly IBufferedCipher m_Cipher;
        private readonly Encoding m_Encoding;

        /// <summary>
        /// Creates a new crypto engine with given parameters.
        /// </summary>
        /// <param name="type">The type of cipher to use</param>
        /// <param name="mode">The cipher block mode to use</param>
        /// <param name="padding">The cipher block padding to use</param>
        /// <param name="encoding">The encoding to use</param>
        public CryptoEngine(CipherType type, CipherMode mode, CipherPadding padding, TextEncoding encoding) {
            m_Type = type;
            m_CipherBlockMode = mode;
            m_Cipher = GetCipher(type, mode, GetCipherPadding(padding));
            m_Encoding = GetEncoding(encoding);
        }

        /// <summary>
        /// Encrypts a plain message with the initilaized mode and returns the result encoded in Base64.
        /// </summary>
        /// <param name="message">The message to encrypt</param>
        /// <param name="key">The key to use</param>
        /// <param name="iv">The initilization vetor</param>
        /// <returns>The encrypted cipher</returns>
        public byte[] Encrypt(string message, byte[] key, byte[] iv) {
            ICipherParameters parameters = GetCipherParameters(key, iv);
            m_Cipher.Init(true, parameters);

            byte[] result = m_Cipher.DoFinal(m_Encoding.GetBytes(message));

            return result;
        }

        /// <summary>
        /// Decrypts a given cipher encoded in Base64 an returns the plain message.
        /// </summary>
        /// <param name="cipher">The encrypted cipher</param>
        /// <param name="key">The key to use</param>
        /// <param name="iv">The initilization vetor</param>
        /// <returns>The plain message</returns>
        public string Decrypt(byte[] cipher, byte[] key, byte[] iv) {
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

        /// <summary>
        /// Generates an initilization vector.
        /// </summary>
        /// <returns>The generated initilization vector</returns>
        public byte[] GenerateIV() {
            byte[] iv = new byte[m_Cipher.GetBlockSize()];
            new SecureRandom().NextBytes(iv);
            return iv;
        }

        private IBufferedCipher GetCipher(CipherType type, CipherMode mode, IBlockCipherPadding padding) {
            if (type == CipherType.Block) {
                switch (mode) {
                    case CipherMode.ECB: return padding == null ? new BufferedBlockCipher(BLOCK_CIPHER_ENGINE) : new PaddedBufferedBlockCipher(BLOCK_CIPHER_ENGINE, padding);
                    case CipherMode.CBC: return padding == null ? new BufferedBlockCipher(new CbcBlockCipher(BLOCK_CIPHER_ENGINE)) : new PaddedBufferedBlockCipher(new CbcBlockCipher(BLOCK_CIPHER_ENGINE), padding);
                    case CipherMode.CTS: return new CtsBlockCipher(new CbcBlockCipher(BLOCK_CIPHER_ENGINE));
                    case CipherMode.CTR: return new BufferedBlockCipher(new SicBlockCipher(BLOCK_CIPHER_ENGINE));
                    case CipherMode.CFB: return new BufferedBlockCipher(new CfbBlockCipher(BLOCK_CIPHER_ENGINE, STREAM_BLOCK_SIZE));
                    case CipherMode.OFB: return new BufferedBlockCipher(new OfbBlockCipher(BLOCK_CIPHER_ENGINE, STREAM_BLOCK_SIZE));
                    default: throw new ArgumentOutOfRangeException(nameof(mode));
                }
            } else {
                return new BufferedStreamCipher(STREAM_CIPHER_ENGINE);
            }
        }

        private IBlockCipherPadding GetCipherPadding(CipherPadding padding) {
            switch (padding) {
                case CipherPadding.None: return null;
                case CipherPadding.ISO7816d4: return new ISO7816d4Padding();
                case CipherPadding.ISO10126d2: return new ISO10126d2Padding();
                case CipherPadding.PKCS5: return new Pkcs7Padding();
                case CipherPadding.PKCS7: return new Pkcs7Padding();
                case CipherPadding.TCB: return new TbcPadding();
                case CipherPadding.X923: return new X923Padding();
                case CipherPadding.ZeroBytes: return new ZeroBytePadding();
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
            if (iv == null || m_Type == CipherType.Stream || m_CipherBlockMode == CipherMode.ECB) {
                return result;
            } else {
                return new ParametersWithIV(result, iv);
            }
        }
    }
}
