﻿using System;
using System.Linq;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace SecureTextEditor.Crypto.Cipher {
    /// <summary>
    /// Cryptographic engine abstracting a block (AES) and stream (RC4) cipher.
    /// </summary>
    public class CipherEngine {
        /// <summary>
        /// Describes the status of the decryption.
        /// </summary>
        public enum DecryptStatus {
            /// <summary>
            /// Describes that the decryption was successfull.
            /// </summary>
            Success,
            /// <summary>
            /// Describes that the decryption failed because the underlying mac reported an error.
            /// </summary>
            MacFailed,
            /// <summary>
            /// Describes that the decryption failed because of an internal error.
            /// </summary>
            Failed
        }

        /// <summary>
        /// Data holder for the result of the decryption operation.
        /// </summary>
        public class DecryptResult {
            /// <summary>
            /// The status of the decryption.
            /// </summary>
            public DecryptStatus Status { get; }
            /// <summary>
            /// The underlying exception that was raised (if any).
            /// </summary>
            public Exception Exception { get; }
            /// <summary>
            /// The actual result of the decryption operation (if any).
            /// </summary>
            public byte[] Result { get; }

            /// <summary>
            /// Constructs a new decryption result object with given parameters.
            /// </summary>
            /// <param name="status">The status of the decryption</param>
            /// <param name="exception">The underlying exception that was raised (if any)</param>
            /// <param name="result">The actual result of the decryption operation (if any)</param>
            public DecryptResult(DecryptStatus status, Exception exception, byte[] result) {
                Status = status;
                Exception = exception;
                Result = result;
            }
        }

        /// <summary>
        /// The size of a block in AES encryption.
        /// </summary>
        public const int BLOCK_SIZE = 16;
        /// <summary>
        /// The keys accepted in AES encryption.
        /// </summary>
        public static readonly int[] AES_ACCEPTED_KEYS = new int[] { 128, 192, 256 };
        /// <summary>
        /// The keys accepted in RC4 encryption.
        /// </summary>
        public static readonly int[] RC4_ACCEPTED_KEYS = new int[] { 128, 160, 192, 256, 512, 1024, 2048 };
        /// <summary>
        /// The size of the tag used in GCM and CCM mode.
        /// </summary>
        private const int GCM_TAG_SIZE = 128;
        /// <summary>
        /// The size of the special nonce used in CCM mode that must be between 7 and 13 octets.
        /// </summary>
        private const int CCM_NONCE_SIZE = 13;

        /// <summary>
        /// The underlying block cipher used in block type.
        /// </summary>
        private static readonly IBlockCipher BLOCK_CIPHER_ENGINE = new AesEngine();
        /// <summary>
        /// The underlying stream cipher used in stream type.
        /// </summary>
        private static readonly IStreamCipher STREAM_CIPHER_ENGINE = new RC4Engine();

        /// <summary>
        /// The type of cipher that is used.
        /// </summary>
        private readonly CipherType m_Type;
        /// <summary>
        /// The block cipher mode that is used.
        /// </summary>
        private readonly CipherMode m_CipherMode;
        /// <summary>
        /// The actual concrete cipher that will be used for encrypting and decrypting.
        /// </summary>
        private readonly IBufferedCipher m_Cipher;

        /// <summary>
        /// Creates a new crypto engine with given parameters.
        /// </summary>
        /// <param name="type">The type of cipher to use</param>
        /// <param name="mode">The cipher block mode to use</param>
        /// <param name="padding">The cipher block padding to use</param>
        /// <param name="encoding">The encoding to use</param>
        public CipherEngine(CipherType type, CipherMode mode, CipherPadding padding) {
            m_Type = type;
            m_CipherMode = mode;
            m_Cipher = GetCipher(type, mode, GetCipherPadding(padding));
        }

        /// <summary>
        /// Encrypts a plain message with the initilaized mode and returns the result.
        /// </summary>
        /// <param name="message">The message to encrypt</param>
        /// <param name="key">The key to use</param>
        /// <param name="iv">The initilization vetor (Can be null if not needed)</param>
        /// <returns>The encrypted cipher</returns>
        public byte[] Encrypt(byte[] message, byte[] key, byte[] iv) {
            ICipherParameters parameters = GenerateCipherParameters(key, iv);
            m_Cipher.Init(true, parameters);

            byte[] result = m_Cipher.DoFinal(message);

            return result;
        }

        /// <summary>
        /// Decrypts a given cipher and returns a result object
        /// which contains information about the status and output of the operation.
        /// </summary>
        /// <param name="cipher">The encrypted cipher</param>
        /// <param name="key">The key to use</param>
        /// <param name="iv">The initilization vetor (Can be null if not needed)</param>
        /// <returns>The result of the decrypt operation</returns>
        public DecryptResult Decrypt(byte[] cipher, byte[] key, byte[] iv) {
            ICipherParameters parameters = GenerateCipherParameters(key, iv);
            m_Cipher.Init(false, parameters);

            byte[] result = new byte[m_Cipher.GetOutputSize(cipher.Length)];
            int length = m_Cipher.ProcessBytes(cipher, 0, cipher.Length, result, 0);

            try {
                length += m_Cipher.DoFinal(result, length);
                return new DecryptResult(DecryptStatus.Success, null, result.Take(length).ToArray());
            } catch(InvalidCipherTextException e) {
                return new DecryptResult(DecryptStatus.MacFailed, e, null);
            } catch (Exception e) {
                return new DecryptResult(DecryptStatus.Failed, e, null);
            }
        }

        /// <summary>
        /// Generates a key for use with this engine.
        /// </summary>
        /// <param name="keySize">The size of the key</param>
        /// <returns>The generated key</returns>
        public byte[] GenerateKey(int keySize) {
            if (m_Type == CipherType.Block && !AES_ACCEPTED_KEYS.Contains(keySize)) {
                throw new ArgumentException("Invalid key size", nameof(keySize));
            }
            if (m_Type == CipherType.Stream && !RC4_ACCEPTED_KEYS.Contains(keySize)) {
                throw new ArgumentException("Invalid key size", nameof(keySize));
            }

            return Generator.GenerateKey(keySize);
        }

        /// <summary>
        /// Generates an initilization vector if the engine requires one otherwise returns null.
        /// </summary>
        /// <returns>The generated initilization vector or null</returns>
        public byte[] GenerateIV() {
            if (m_CipherMode == CipherMode.ECB) {
                return null;
            } else {
                byte[] iv = new byte[m_CipherMode == CipherMode.CCM ? CCM_NONCE_SIZE : m_Cipher.GetBlockSize()];
                new SecureRandom().NextBytes(iv);
                return iv;
            }
        }

        /// <summary>
        /// Converts cipher type, mode and padding into an actual cipher to use.
        /// </summary>
        /// <param name="type">The type of cipher to use</param>
        /// <param name="mode">The cipher block mode to use</param>
        /// <param name="padding">The cipher block padding to use</param>
        /// <returns>The cipher</returns>
        private IBufferedCipher GetCipher(CipherType type, CipherMode mode, IBlockCipherPadding padding) {
            if (type == CipherType.Block) {
                switch (mode) {
                    case CipherMode.ECB: return padding == null ? new BufferedBlockCipher(BLOCK_CIPHER_ENGINE) : new PaddedBufferedBlockCipher(BLOCK_CIPHER_ENGINE, padding);
                    case CipherMode.CBC: return padding == null ? new BufferedBlockCipher(new CbcBlockCipher(BLOCK_CIPHER_ENGINE)) : new PaddedBufferedBlockCipher(new CbcBlockCipher(BLOCK_CIPHER_ENGINE), padding);
                    case CipherMode.CTS: return new CtsBlockCipher(new CbcBlockCipher(BLOCK_CIPHER_ENGINE));
                    case CipherMode.CTR: return new BufferedBlockCipher(new SicBlockCipher(BLOCK_CIPHER_ENGINE));
                    case CipherMode.CFB: return new BufferedBlockCipher(new CfbBlockCipher(BLOCK_CIPHER_ENGINE, BLOCK_SIZE));
                    case CipherMode.OFB: return new BufferedBlockCipher(new OfbBlockCipher(BLOCK_CIPHER_ENGINE, BLOCK_SIZE));
                    case CipherMode.GCM: return new BufferedAeadBlockCipher(new GcmBlockCipher(BLOCK_CIPHER_ENGINE));
                    case CipherMode.CCM: return new BufferedAeadBlockCipher(new CcmBlockCipher(BLOCK_CIPHER_ENGINE));
                    default: throw new ArgumentOutOfRangeException(nameof(mode));
                }
            } else {
                return new BufferedStreamCipher(STREAM_CIPHER_ENGINE);
            }
        }

        /// <summary>
        /// Converts cipher padding into an actual padding to use.
        /// </summary>
        /// <param name="padding">The padding</param>
        /// <returns>The cipher padding</returns>
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

        /// <summary>
        /// Generates cipher parameters for the engine to operate with.
        /// </summary>
        /// <param name="key">The cryptographic key</param>
        /// <param name="iv">The initilization vector (or nonce)</param>
        /// <returns>The generated cipher parameters</returns>
        private ICipherParameters GenerateCipherParameters(byte[] key, byte[] iv) {
            KeyParameter keyParameter = new KeyParameter(key);
            if (iv == null || m_Type == CipherType.Stream || m_CipherMode == CipherMode.ECB) {
                return keyParameter;
            } else if (m_CipherMode == CipherMode.GCM || m_CipherMode == CipherMode.CCM) {
                return new AeadParameters(keyParameter, GCM_TAG_SIZE, iv);
            } else {
                return new ParametersWithIV(keyParameter, iv);
            }
        }
    }
}