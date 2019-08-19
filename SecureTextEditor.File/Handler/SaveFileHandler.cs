using System;
using System.IO;
using System.Security;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SecureTextEditor.Crypto;
using SecureTextEditor.Crypto.Cipher;
using SecureTextEditor.Crypto.Digest;
using SecureTextEditor.Crypto.Signature;
using SecureTextEditor.Crypto.Storage;
using SecureTextEditor.File.Options;

namespace SecureTextEditor.File.Handler {
    /// <summary>
    /// The file handler for saving a file.
    /// </summary>
    public class SaveFileHandler : FileHandler {
        /// <summary>
        /// The alias to save the private signature key under in the key storage.
        /// </summary>
        private const string SIGNATURE_PRIVATE_KEY_ALIAS = "signature_private_key";

        /// <summary>
        /// Saves a file with given parameters asynchronously.
        /// </summary>
        /// <param name="parameters">The save file parameters to use</param>
        /// <returns>The result of the save operation</returns>
        public async Task<SaveFileResult> SaveFileAsync(SaveFileParameters parameters) {
            return await Task.Run(() => {
                try {
                    string path = parameters.Path;
                    TextEncoding encoding = parameters.Encoding;
                    EncryptionOptions options = parameters.EncryptionOptions;
                    SecureString password = parameters.PBEPassword;
                    SecureString keyStoragePassword = parameters.KeyStoragePassword;
                    string fileName = Path.GetFileName(path);

                    byte[] encodedText = GetEncoding(encoding).GetBytes(parameters.Text);
                    byte[] messageToEncrypt = encodedText;
                    byte[] macKey = null;

                    if (options.DigestType != DigestType.None) {
                        // We compute the digest from plain message
                        DigestEngine digestEngine = new DigestEngine(options.DigestType);
                        macKey = digestEngine.GenerateKey();
                        byte[] digest = digestEngine.Digest(encodedText, macKey);

                        // Append the digest to the text
                        messageToEncrypt = new byte[encodedText.Length + digest.Length];
                        Buffer.BlockCopy(encodedText, 0, messageToEncrypt, 0, encodedText.Length);
                        Buffer.BlockCopy(digest, 0, messageToEncrypt, encodedText.Length, digest.Length);
                    }

                    // Encrypt text and save file
                    CipherEngine cipherEngine = CreateCryptoEngine(options);
                    byte[] ivOrSalt = cipherEngine.GenerateIV();
                    byte[] cipherKey = null;
                    password.Process(chars => cipherKey = cipherEngine.GenerateKey(options.CipherKeyOption == CipherKeyOption.Generate ? null : chars, ivOrSalt));
                    byte[] cipher = cipherEngine.Encrypt(messageToEncrypt, cipherKey, ivOrSalt);

                    // We overwrite the current key size with the correct one
                    options.CipherKeySize = cipherEngine.KeySize;

                    // Sign the cipher
                    SignatureKeyPair keyPair = null;
                    byte[] sign = null;
                    byte[] publicKey = null;
                    {
                        if (options.SignatureType != SignatureType.None) {
                            // Load the key storage into memory
                            KeyStorage keyStorage = new KeyStorage(parameters.KeyStoragePath);
                            KeyStorageLoadResult result = null;
                            keyStoragePassword.Process(chars => result = keyStorage.Load(chars));
                            if (result.Status == KeyStorageLoadStatus.PasswordWrong) {
                                return new SaveFileResult(SaveFileStatus.KeyStoragePasswordWrong, result.Exception, null);
                            } else if (result.Status == KeyStorageLoadStatus.Failed) {
                                return new SaveFileResult(SaveFileStatus.Failed, result.Exception, null);
                            }

                            // Check if we can use the key storage or need to generate a fresh key pair
                            SignatureEngine signatureEngine = new SignatureEngine(options.SignatureType, options.SignatureKeySize);
                            if (keyStorage.Exists(SIGNATURE_PRIVATE_KEY_ALIAS)) {
                                keyPair = keyStorage.Retrieve(SIGNATURE_PRIVATE_KEY_ALIAS);
                            } else {
                                keyPair = signatureEngine.GenerateKeyPair();
                                // Save the pair in the storage
                                keyStorage.Store(SIGNATURE_PRIVATE_KEY_ALIAS, keyPair);
                                keyStoragePassword.Process(chars => keyStorage.Save(chars));
                            }

                            sign = signatureEngine.Sign(cipher, keyPair.PrivateKey);
                            publicKey = keyPair.PublicKey;
                        }
                    }

                    // Save the actual secure text file
                    SecureTextFile file = new SecureTextFile(
                        encoding,
                        options,
                        ConvertToBase64OrNull(cipher),
                        ConvertToBase64OrNull(ivOrSalt),
                        ConvertToBase64OrNull(publicKey),
                        ConvertToBase64OrNull(sign)
                    );
                    SaveSecureTextFile(path, file);

                    // Clear out signature key pair
                    if (keyPair != null) {
                        keyPair.Clear();
                    }

                    // Save cipher key into file next to the text file and clear it
                    System.IO.File.WriteAllBytes(ResolvePathForCipherKeyFile(path), cipherKey);
                    cipherKey.Clear();

                    // If we have a MAC key to save, save it to a seperate file as well and clear it
                    if (options.DigestType != DigestType.None) {
                        if (macKey != null) {
                            System.IO.File.WriteAllBytes(ResolvePathForCipherKeyFile(path), macKey);
                            macKey.Clear();
                        }
                    }

                    FileMetaData fileMetaData = new FileMetaData() {
                        Encoding = encoding,
                        EncryptionOptions = options,
                        FileName = fileName,
                        FilePath = path,
                    };

                    return new SaveFileResult(SaveFileStatus.Success, null, fileMetaData);
                } catch (Exception e) {
                    return new SaveFileResult(SaveFileStatus.Failed, e, null);
                };
            });
        }

        /// <summary>
        /// Saves a secure text file to disk.
        /// </summary>
        /// <param name="path">The path to the save location</param>
        /// <param name="file">The secure text file to save</param>
        private static void SaveSecureTextFile(string path, SecureTextFile file) {
            string json = JsonConvert.SerializeObject(file, SERIALIZER_SETTINGS);
            System.IO.File.WriteAllText(path, json);
        }

        /// <summary>
        /// Converts a given byte array to a Base64 string.
        /// </summary>
        /// <param name="value">The byte arry to convert or null</param>
        /// <returns>The Base64 string or null if the byte array was null</returns>
        private static string ConvertToBase64OrNull(byte[] value) {
            return value == null ? null : Convert.ToBase64String(value);
        }
    }
}