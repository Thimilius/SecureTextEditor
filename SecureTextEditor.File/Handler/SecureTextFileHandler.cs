using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SecureTextEditor.Crypto;
using SecureTextEditor.Crypto.Cipher;
using SecureTextEditor.Crypto.Digest;
using SecureTextEditor.Crypto.Signature;
using SecureTextEditor.File.Options;

// TODO: Finish xml docs

namespace SecureTextEditor.File.Handler {
    /// <summary>
    /// Handler that abstracts opening and loading a secure text file.
    /// </summary>
    public class SecureTextFileHandler : IFileHandler {
        /// <summary>
        /// The extension used for the file.
        /// </summary>
        public const string STXT_FILE_EXTENSION = ".stxt";
        /// <summary>
        /// The extension used for the cipher key file.
        /// </summary>
        public const string CIPHER_KEY_FILE_EXTENSION = ".key";
        /// <summary>
        /// The extension used for the mac key file.
        /// </summary>
        public const string MAC_KEY_FILE_EXTENSION = ".mackey";
        
        /// <summary>
        /// Settings for serializing and deserializing the text file.
        /// </summary>
        private static readonly JsonSerializerSettings SERIALIZER_SETTINGS = new JsonSerializerSettings() {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Auto,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter>() { new StringEnumConverter() }
        };

        /// <summary>
        /// <see cref="IFileHandler.SaveFileAsync"/>
        /// </summary>
        public async Task<SaveFileResult> SaveFileAsync(SaveFileParameters parameters) {
            try {
                string path = parameters.Path;
                TextEncoding encoding = parameters.Encoding;
                EncryptionOptions options = parameters.EncryptionOptions;
                SecureString password = parameters.Password;
                string fileName = Path.GetFileName(path);

                await Task.Run(() => {
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
                    CipherEngine cipherEngine = GetCryptoEngine(options);
                    byte[] ivOrSalt = cipherEngine.GenerateIV();
                    byte[] cipherKey = password.Process(chars => cipherEngine.GenerateKey(options.CipherKeyOption == CipherKeyOption.Generate ? null : chars, ivOrSalt));
                    byte[] cipher = cipherEngine.Encrypt(messageToEncrypt, cipherKey, ivOrSalt);

                    // We overwrite the current key size with the correct one
                    options.CipherKeySize = cipherEngine.KeySize;

                    // Sign the cipher
                    SignatureKeyPair keyPair = null;
                    byte[] sign = null;
                    if (options.SignatureType != SignatureType.None) {
                        SignatureEngine signaturEngine = new SignatureEngine(options.SignatureType, options.SignatureKeySize);
                        keyPair = signaturEngine.GenerateKeyPair();
                        sign = signaturEngine.Sign(cipher, keyPair.PrivateKey);
                    }

                    // Save the actual secure text file
                    SecureTextFile file = new SecureTextFile(
                        options,
                        encoding,
                        ConvertToBase64OrNull(ivOrSalt),
                        ConvertToBase64OrNull(keyPair.PublicKey),
                        ConvertToBase64OrNull(sign),
                        ConvertToBase64OrNull(cipher)
                    );
                    SaveSecureTextFile(path, file);

                    // Clear out signature key pair
                    if (keyPair != null) {
                        keyPair.Clear();
                    }

                    // Save cipher key into file next to the text file and clear it
                    System.IO.File.WriteAllBytes(GetPathForCipherKeyFile(path), cipherKey);
                    cipherKey.Clear();

                    // If we have a mac key to save, save it to a seperate file as well and clear it
                    if (options.DigestType != DigestType.None) {
                        if (macKey != null) {
                            System.IO.File.WriteAllBytes(GetPathForMacKeyFile(path), macKey);
                            macKey.Clear();
                        }
                    }
                });
                await Task.Delay(250);

                FileMetaData fileMetaData = new FileMetaData() {
                    Encoding = encoding,
                    EncryptionOptions = options,
                    FileName = fileName,
                    FilePath = path,
                };

                return new SaveFileResult(SaveFileStatus.Success, null, fileMetaData);
            } catch(Exception e) {
                return new SaveFileResult(SaveFileStatus.Failed, e, null);
            }
        }

        /// <summary>
        /// <see cref="IFileHandler.OpenFile"/>
        /// </summary>
        public OpenFileResult OpenFile(OpenFileParameters parameters) {
            try {
                string path = parameters.Path;
                string fileName = Path.GetFileName(path);

                // Load file and decrypt with corresponding encoding
                SecureTextFile textFile = LoadSecureTextFile<SecureTextFile>(path);
                TextEncoding encoding = textFile.Encoding;
                EncryptionOptions options = textFile.EncryptionOptions;
                byte[] cipher = Convert.FromBase64String(textFile.Base64Cipher);
                byte[] iv = textFile.Base64IVOrSalt != null ? Convert.FromBase64String(textFile.Base64IVOrSalt) : null;

                CipherEngine cipherEngine = GetCryptoEngine(options);

                // Get the key from a file or get it from a password in case of password based encryption
                byte[] cipherKey = null;
                {
                    if (options.CipherKeyOption == CipherKeyOption.Generate) {
                        // Try loading in the key file at the same location
                        string cipherKeyPath = GetPathForCipherKeyFile(path);
                        if (!System.IO.File.Exists(cipherKeyPath)) {
                            cipherKeyPath = parameters.CipherKeyFileResolver?.Invoke(options.CipherKeySize);
                            // If no path was supplied, we bail out
                            if (cipherKeyPath == null) {
                                return new OpenFileResult(OpenFileStatus.Canceled);
                            }
                        }
                        cipherKey = System.IO.File.ReadAllBytes(cipherKeyPath);
                    } else {
                        SecureString password = parameters.PasswordResolver?.Invoke();
                        // If no password was supplied, we bail out
                        if (password == null) {
                            return new OpenFileResult(OpenFileStatus.Canceled);
                        } else {
                            cipherKey = password.Process(chars => cipherEngine.GenerateKey(chars, iv));
                        }
                    }
                }

                // Try loading in the mac key if we need it
                byte[] macKey = null;
                {
                    if (options.DigestType == DigestType.AESCMAC || options.DigestType == DigestType.HMACSHA256) {
                        string macKeyPath = GetPathForMacKeyFile(path);
                        if (!System.IO.File.Exists(macKeyPath)) {
                            macKeyPath = parameters.MacKeyFileResolver?.Invoke();
                            // If no path was supplied, we bail out
                            if (macKeyPath == null) {
                                return new OpenFileResult(OpenFileStatus.Canceled);
                            }
                        }
                        macKey = System.IO.File.ReadAllBytes(macKeyPath);
                    }
                }

                // Verify signature
                {
                    if (options.SignatureType != SignatureType.None) {
                        SignatureEngine signatureEngine = new SignatureEngine(options.SignatureType, options.SignatureKeySize);
                        if (!signatureEngine.Verify(cipher, Convert.FromBase64String(textFile.Base64Signature), Convert.FromBase64String(textFile.Base64SignatureKey))) {
                            return new OpenFileResult(OpenFileStatus.SignatureFailed);
                        }
                    }
                }

                // Decrypt cipher
                CipherDecryptResult decryptResult = cipherEngine.Decrypt(cipher, cipherKey, iv);

                // Clear out the cipher key because we no longer need it
                cipherKey.Clear();

                if (decryptResult.Status == CipherDecryptStatus.MacFailed) {
                    return new OpenFileResult(OpenFileStatus.MacFailed, decryptResult.Exception);
                } else if (decryptResult.Status == CipherDecryptStatus.Failed) {
                    return new OpenFileResult(OpenFileStatus.Failed, decryptResult.Exception);
                }

                // Reverse the appending of the hash if needed
                byte[] messageDecrypted = decryptResult.Result;
                byte[] message = messageDecrypted;
                {
                    if (options.DigestType != DigestType.None) {
                        DigestEngine digestEngine = new DigestEngine(options.DigestType);

                        // We need to extract the hash from the cipher
                        int digestLength = digestEngine.GetDigestLength();
                        int messageLength = messageDecrypted.Length - digestLength;
                        message = messageDecrypted.Take(messageLength).ToArray();
                        byte[] digest = messageDecrypted.Skip(messageLength).ToArray();

                        // Compare saved and new computed digest
                        byte[] newDigest = digestEngine.Digest(message, macKey);

                        // Clear out mac key if we had one because we no longer need it
                        if (macKey != null) {
                            macKey.Clear();
                        }

                        if (!DigestEngine.AreEqual(newDigest, digest)) {
                            return new OpenFileResult(OpenFileStatus.MacFailed, decryptResult.Exception);
                        }
                    }
                }

                string text = GetEncoding(textFile.Encoding).GetString(message);

                FileMetaData fileMetaData = new FileMetaData() {
                    Encoding = encoding,
                    EncryptionOptions = options,
                    FileName = fileName,
                    FilePath = path,
                };
                return new OpenFileResult(OpenFileStatus.Success, null, fileMetaData, text);
            } catch (Exception e) {
                return new OpenFileResult(OpenFileStatus.Failed, e);
            }
        }

        private static void SaveSecureTextFile(string path, SecureTextFile file) {
            string json = JsonConvert.SerializeObject(file, SERIALIZER_SETTINGS);
            System.IO.File.WriteAllText(path, json);
        }

        private static SecureTextFile LoadSecureTextFile<T>(string path) {
            string json = System.IO.File.ReadAllText(path);
            return JsonConvert.DeserializeObject<SecureTextFile>(json, SERIALIZER_SETTINGS);
        }

        private static CipherEngine GetCryptoEngine(EncryptionOptions options) {
            if (options is EncryptionOptionsAES optionsAES) {
                return new CipherEngine(optionsAES.CipherType, optionsAES.CipherMode, optionsAES.CipherPadding, options.CipherKeyOption, options.CipherKeySize);
            } else if (options is EncryptionOptionsRC4 optionsRC4) {
                return new CipherEngine(optionsRC4.CipherType, CipherMode.None, CipherPadding.None, options.CipherKeyOption, options.CipherKeySize);
            } else {
                return null;
            }
        }

        private static Encoding GetEncoding(TextEncoding encoding) {
            switch (encoding) {
                case TextEncoding.ASCII: return Encoding.ASCII;
                case TextEncoding.UTF8: return Encoding.UTF8;
                default: throw new InvalidOperationException();
            }
        }

        private static string ConvertToBase64OrNull(byte[] value) {
            return value == null ? null : Convert.ToBase64String(value);
        }

        private static string GetPathForCipherKeyFile(string basePath) {
            return Path.GetFileNameWithoutExtension(basePath) + CIPHER_KEY_FILE_EXTENSION;
        }

        private static string GetPathForMacKeyFile(string basePath) {
            return Path.GetFileNameWithoutExtension(basePath) + MAC_KEY_FILE_EXTENSION;
        }
    }
}
