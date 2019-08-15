using System;
using System.Linq;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Digests;

namespace SecureTextEditor.Crypto.Cipher {
    /// <summary>
    /// Cryptographic engine abstracting a block (AES) and stream (RC4) cipher.
    /// </summary>
    public class CipherEngine {
        /// <summary>
        /// The key sizes accepted in AES encryption.
        /// </summary>
        public static readonly int[] AES_ACCEPTED_KEYS = new int[] { 128, 192, 256 };
        /// <summary>
        /// The key sizes accepted in RC4 encryption.
        /// </summary>
        public static readonly int[] RC4_ACCEPTED_KEYS = new int[] { 40, 64, 128, 160, 192, 256, 512, 1024, 2048 };
        /// <summary>
        /// The size of a block in AES encryption.
        /// </summary>
        private const int BLOCK_SIZE = 16;
        /// <summary>
        /// The size of the tag used in authenticated enryption modes (GCM and CCM).
        /// </summary>
        private const int AE_TAG_SIZE = 128;
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
        /// Gets the size of the cryptographic key used.
        /// </summary>
        public int KeySize { get; }

        /// <summary>
        /// The type of cipher that is used.
        /// </summary>
        private readonly CipherType m_Type;
        /// <summary>
        /// The block cipher mode that is used.
        /// </summary>
        private readonly CipherMode m_CipherMode;
        /// <summary>
        /// The type of key that is used.
        /// </summary>
        private readonly CipherKeyOption m_KeyOption;
        /// <summary>
        /// The actual concrete cipher that will be used for encrypting and decrypting.
        /// </summary>
        private readonly IBufferedCipher m_Cipher;

        /// <summary>
        /// Creates a new crypto engine with given parameters.
        /// </summary>
        /// <param name="type">The type of cipher to use</param>
        /// <param name="option">The key option to use</param>
        /// <param name="mode">The cipher block mode to use</param>
        /// <param name="padding">The cipher block padding to use</param>
        /// <param name="encoding">The encoding to use</param>
        /// <param name="wantedKeySize">The wanted key size</param>
        public CipherEngine(CipherType type, CipherMode mode, CipherPadding padding, CipherKeyOption option, int wantedKeySize) {
            ValidateParameters(type, mode, padding, option, wantedKeySize);

            m_Type = type;
            m_CipherMode = mode;
            m_Cipher = GetCipher(type, mode, GetCipherPadding(padding));
            m_KeyOption = option;
            KeySize = GetKeySize(type, option, wantedKeySize);
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
        public CipherDecryptResult Decrypt(byte[] cipher, byte[] key, byte[] iv) {
            ICipherParameters parameters = GenerateCipherParameters(key, iv);
            m_Cipher.Init(false, parameters);

            byte[] result = new byte[m_Cipher.GetOutputSize(cipher.Length)];
            int length = m_Cipher.ProcessBytes(cipher, 0, cipher.Length, result, 0);

            try {
                length += m_Cipher.DoFinal(result, length);
                return new CipherDecryptResult(CipherDecryptStatus.Success, null, result.Take(length).ToArray());
            } catch(InvalidCipherTextException e) {
                // This is a little hacky way of determining the actual error
                // but we don't really have control over that
                if (e.Message == "pad block corrupted") {
                    return new CipherDecryptResult(CipherDecryptStatus.MacFailed, e, null);
                } else {
                    return new CipherDecryptResult(CipherDecryptStatus.Failed, e, null);
                }
            } catch (Exception e) {
                return new CipherDecryptResult(CipherDecryptStatus.Failed, e, null);
            }
        }

        /// <summary>
        /// Generates a key for use with this engine.
        /// </summary>
        /// <param name="password">The password required for password based encryption (Can be null if not needed)</param>
        /// <param name="salt">The salt that is needed for key generation (Can be null if not needed)</param>
        /// <returns>The generated key</returns>
        public byte[] GenerateKey(char[] password, byte[] salt) {
            switch (m_KeyOption) {
                case CipherKeyOption.Generate:
                    // Simple cipher key generation
                    CipherKeyGenerator generator = new CipherKeyGenerator();
                    generator.Init(new KeyGenerationParameters(new SecureRandom(), KeySize));
                    return generator.GenerateKey();
                case CipherKeyOption.PBE:
                    // Generate key from password (depending on cipher type)
                    PbeParametersGenerator pbeGenerator = null;
                    string algorithm = null;
                    if (m_Type == CipherType.AES) {
                        pbeGenerator = new Pkcs12ParametersGenerator(new Sha256Digest());
                        algorithm = "AES";
                    } else if (m_Type == CipherType.RC4) {
                        pbeGenerator = new Pkcs12ParametersGenerator(new Sha1Digest());
                        algorithm = "RC4";
                    }
                    int iterationCount = 2048;
                    pbeGenerator.Init(PbeParametersGenerator.Pkcs12PasswordToBytes(password), salt, iterationCount);
                    return ((KeyParameter)pbeGenerator.GenerateDerivedParameters(algorithm, KeySize)).GetKey();
                case CipherKeyOption.PBEWithSCRYPT:
                    // Generate cipher key from password with SCRYPT
                    byte[] encoded = PbeParametersGenerator.Pkcs5PasswordToUtf8Bytes(password);
                    int keySize = KeySize / 8; // Needs to be in bytes
                    int costParameterN = 2048; // Needs to a power of 2
                    int blockSize = m_Cipher.GetBlockSize();
                    int parallelisationParameter = 1; // Depends on block size
                    return SCrypt.Generate(encoded, salt, costParameterN, blockSize, parallelisationParameter, keySize);
                default: throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Generates an initilization vector or salt if the engine requires one otherwise returns null.
        /// </summary>
        /// <returns>The generated initilization vector or salt</returns>
        public byte[] GenerateIV() {
            if (m_Type == CipherType.RC4 || m_CipherMode == CipherMode.ECB) {
                return null;
            } else {
                // If we use CCM the iv is treated as the nonce
                int size = m_CipherMode == CipherMode.CCM ? CCM_NONCE_SIZE : m_Cipher.GetBlockSize();
                byte[] iv = new byte[size];
                new SecureRandom().NextBytes(iv);
                return iv;
            }
        }

        /// <summary>
        /// Checks for a given message wether or not CTS padding is possible.
        /// </summary>
        /// <param name="message">The message to check</param>
        /// <returns>True if CTS padding is possible otherwise false</returns>
        public static bool IsCTSPaddingPossible(string message) {
            return message.Length >= BLOCK_SIZE;
        }

        /// <summary>
        /// Converts cipher type, mode and padding into an actual cipher to use.
        /// </summary>
        /// <param name="type">The type of cipher to use</param>
        /// <param name="mode">The cipher block mode to use</param>
        /// <param name="padding">The cipher block padding to use</param>
        /// <returns>The cipher</returns>
        private IBufferedCipher GetCipher(CipherType type, CipherMode mode, IBlockCipherPadding padding) {
            if (type == CipherType.AES) {
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
            switch (m_KeyOption) {
                case CipherKeyOption.Generate:
                    KeyParameter keyParameter = new KeyParameter(key);
                    if (iv == null || m_Type == CipherType.RC4 || m_CipherMode == CipherMode.ECB) {
                        return keyParameter;
                    } else if (m_CipherMode == CipherMode.GCM || m_CipherMode == CipherMode.CCM) {
                        return new AeadParameters(keyParameter, AE_TAG_SIZE, iv);
                    } else {
                        return new ParametersWithIV(keyParameter, iv);
                    }
                case CipherKeyOption.PBE: return new KeyParameter(key);
                case CipherKeyOption.PBEWithSCRYPT:
                    keyParameter = new KeyParameter(key);
                    return new ParametersWithIV(keyParameter, iv);
                default: throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Gets the key size based on the given parameters.
        /// </summary>
        /// <param name="type">The type of cipher</param>
        /// <param name="option">The key option</param>
        /// <param name="wantedKeySize">The wanted key size</param>
        /// <returns>The key size</returns>
        private int GetKeySize(CipherType type, CipherKeyOption option, int wantedKeySize) {
            switch (option) {
                case CipherKeyOption.Generate: return wantedKeySize;
                case CipherKeyOption.PBE: return type == CipherType.AES ? 128 : 40; // PBE either means AES 128 or RC4 40 
                case CipherKeyOption.PBEWithSCRYPT: return 256; // PBEWithSCRYPT means AES 256
                default: throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Validates the engine configuration based on given parameters.
        /// </summary>
        /// <param name="type">The cipher type</param>
        /// <param name="mode">The cipher mode</param>
        /// <param name="padding">The cipher padding</param>
        /// <param name="option">The key option</param>
        /// <param name="wantedKeySize">The wanted key size</param>
        private void ValidateParameters(CipherType type, CipherMode mode, CipherPadding padding, CipherKeyOption option, int wantedKeySize) {
            // Verify key options
            if (option == CipherKeyOption.PBE) {
                if (type == CipherType.AES && mode != CipherMode.CBC) {
                    throw new InvalidOperationException("Key option PBE with AES is only supported with CBC mode!");
                }
            } else if (option == CipherKeyOption.PBEWithSCRYPT) {
                if (type != CipherType.AES || mode != CipherMode.GCM || padding != CipherPadding.None) {
                    throw new InvalidOperationException("Key option PBEWithSCRYPT is only supported via AES with GCM and no padding!");
                }
            }

            // Verify key sizes
            if (type == CipherType.AES && !AES_ACCEPTED_KEYS.Contains(wantedKeySize)) {
                throw new InvalidOperationException($"Invalid key size of {wantedKeySize}!");
            }
            if (type == CipherType.RC4 && !RC4_ACCEPTED_KEYS.Contains(wantedKeySize)) {
                throw new InvalidOperationException($"Invalid key size of {wantedKeySize}!");
            }
        }
    }
}
