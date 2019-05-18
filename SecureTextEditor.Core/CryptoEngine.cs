﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Modes;
using System;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace SecureTextEditor.Core {
    public class CryptoEngine {
        // TODO: We need a stream cipher abstraction
        // TODO: We need a way to specify key size
        // TODO: Generate key and iv instead of being it hardcoded

        private const string KEY = "000102030405060708090a0b0c0d0e0f";
        private const string IV  = "0001020304050607";
        private const int STREAM_BLOCK_SIZE = 16;
        private static readonly IBlockCipher CIPHER_ENGINE = new AesEngine();

        private readonly IBufferedCipher m_Cipher;
        private readonly CipherBlockMode m_CipherBlockMode;
        private readonly Encoding m_Encoding;

        public CryptoEngine(CipherBlockMode mode, CipherBlockPadding padding, TextEncoding encoding) {
            m_CipherBlockMode = mode;
            m_Cipher = GetCipherMode(mode, GetCipherPadding(padding));
            m_Encoding = GetEncoding(encoding);
        }

        /// <summary>
        /// Encrypts a plain message with the initilaized mode and returns the result encoded in Base64.
        /// </summary>
        /// <param name="message">The message to encrypt</param>
        /// <returns>The encrypted cipher encoded in Base64</returns>
        public string Encrypt(string message) {
            byte[] iv = null;
            if (m_CipherBlockMode != CipherBlockMode.ECB) {
                iv = m_Encoding.GetBytes(IV);
            }
            ICipherParameters parameters = GetCipherParameters(m_Encoding.GetBytes(KEY), iv);
            byte[] result = EncryptDecrypt(true, m_Encoding.GetBytes(message), parameters);
            return Convert.ToBase64String(result);
        }

        /// <summary>
        /// Decrypts a given cipher encoded in Base64 an returns the plain message.
        /// </summary>
        /// <param name="cipher">The cipher encoded in Base64 to decrypt</param>
        /// <returns>The plain message</returns>
        public string Decrypt(string cipher) {
            byte[] iv = null;
            if (m_CipherBlockMode != CipherBlockMode.ECB) {
                iv = m_Encoding.GetBytes(IV);
            }
            ICipherParameters parameters = GetCipherParameters(m_Encoding.GetBytes(KEY), iv);
            byte[] result = EncryptDecrypt(false, Convert.FromBase64String(cipher), parameters);
            return m_Encoding.GetString(result);
        }

        private byte[] EncryptDecrypt(bool encrypt, byte[] input, ICipherParameters parameters) {
            m_Cipher.Init(encrypt, parameters);
            return m_Cipher.DoFinal(input);
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

        private IBufferedCipher GetCipherMode(CipherBlockMode mode, IBlockCipherPadding padding) {
            switch (mode) {
                case CipherBlockMode.ECB: return padding == null ? new BufferedBlockCipher(CIPHER_ENGINE) : new PaddedBufferedBlockCipher(CIPHER_ENGINE, padding);
                case CipherBlockMode.CBC: return padding == null ? new BufferedBlockCipher(new CbcBlockCipher(CIPHER_ENGINE)) : new PaddedBufferedBlockCipher(new CbcBlockCipher(CIPHER_ENGINE), padding);
                case CipherBlockMode.CTS: return new CtsBlockCipher(new CbcBlockCipher(CIPHER_ENGINE));
                case CipherBlockMode.CTR: return new BufferedBlockCipher(new SicBlockCipher(CIPHER_ENGINE));
                case CipherBlockMode.CFB: return new BufferedBlockCipher(new CfbBlockCipher(CIPHER_ENGINE, STREAM_BLOCK_SIZE));
                case CipherBlockMode.OFB: return new BufferedBlockCipher(new OfbBlockCipher(CIPHER_ENGINE, STREAM_BLOCK_SIZE));
                default: throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        private Encoding GetEncoding(TextEncoding encoding) {
            switch (encoding) {
                case TextEncoding.ASCII: return Encoding.ASCII;
                case TextEncoding.UTF8: return Encoding.UTF8;
                default: throw new ArgumentOutOfRangeException(nameof(encoding));
            }
        }

        private ICipherParameters GetCipherParameters(byte[] key, byte[] iv = null) {
            ICipherParameters result = new KeyParameter(key);
            if (iv != null) {
                result = new ParametersWithIV(result, iv);
            }
            return result;
        }
    }
}
