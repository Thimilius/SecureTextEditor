using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SecureTextEditor.Crypto.Cipher;
using SecureTextEditor.Crypto.Digest;
using SecureTextEditor.Crypto.Signature;
using SecureTextEditor.File.Options;

namespace SecureTextEditor.File.Handler {
    /// <summary>
    /// Handler to resolve the password used in password based encryption.
    /// </summary>
    /// <returns>The password used in password based encryption</returns>
    public delegate char[] PasswordResolver();
    /// <summary>
    /// Handler to resolve the path to a cipher key file.
    /// </summary>
    /// <param name="keySize">The key size that is to be expected</param>
    /// <returns>The path to the cipher key file to load</returns>
    public delegate string CipherKeyFileResolver(int keySize);
    /// <summary>
    /// Handler to resolve the path to a mac key file.
    /// </summary>
    /// <returns>The path to the mac key file to load</returns>
    public delegate string MacKeyFileResolver();

    /// <summary>
    /// Handler that abstracts opening and loading a secure text file.
    /// </summary>
    public static class FileHandler {
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

        public static async Task<SaveFileResult> SaveFileAsync(string path, EncryptionOptions options, TextEncoding encoding, string text, string password) {
            try {
                string fileName = Path.GetFileName(path);

                await Task.Run(() => {
                    byte[] encodedText = GetEncoding(encoding).GetBytes(text);
                    byte[] messageToEncrypt = encodedText;
                    byte[] macKey = null;

                    if (options.DigestType != DigestType.None) {
                        // We compute the digest from message
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
                    byte[] cipherKey = cipherEngine.GenerateKey(options.KeyOption == CipherKeyOption.Generate ? null : password.ToCharArray());
                    byte[] iv = cipherEngine.GenerateIV();
                    byte[] cipher = cipherEngine.Encrypt(messageToEncrypt, cipherKey, iv);

                    // Sign the cipher
                    SignatureEngine signaturEngine = new SignatureEngine(options.SignatureType, options.SignatureKeySize);
                    SignatureKeyPair keyPair = signaturEngine.GenerateKeyPair();
                    byte[] sign = signaturEngine.Sign(cipher, keyPair.PrivateKey);

                    SecureTextFile textFile = new SecureTextFile(
                        options,
                        encoding,
                        iv != null ? Convert.ToBase64String(iv) : null,
                        Convert.ToBase64String(keyPair.PublicKey),
                        Convert.ToBase64String(sign),
                        Convert.ToBase64String(cipher)
                    );
                    SaveSecureTextFile(path, textFile);

                    // Save cipher key into file next to the text file
                    System.IO.File.WriteAllBytes(GetPathForCipherKeyFile(path), cipherKey);

                    // If we have a mac key to save, save it to a seperate file as well
                    if (options.DigestType != DigestType.None) {
                        if (macKey != null) {
                            System.IO.File.WriteAllBytes(GetPathForMacKeyFile(path), macKey);
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

        public static OpenFileResult OpenFile(string path, PasswordResolver passwordResolver, CipherKeyFileResolver cipherKeyFileResolver, MacKeyFileResolver macKeyFileResolver) {
            try {
                string fileName = Path.GetFileName(path);

                // Load file and decrypt with corresponding encoding
                SecureTextFile textFile = LoadSecureTextFile<SecureTextFile>(path);
                TextEncoding encoding = textFile.Encoding;
                EncryptionOptions options = textFile.EncryptionOptions;
                byte[] cipher = Convert.FromBase64String(textFile.Base64Cipher);

                CipherEngine cipherEngine = GetCryptoEngine(options);

                // Get the key from a file or get it from a password in case of password based encryption
                byte[] cipherKey = null;
                if (options.KeyOption == CipherKeyOption.Generate) {
                    // Try loading in the key file at the same location
                    string cipherKeyPath = GetPathForCipherKeyFile(path);
                    if (!System.IO.File.Exists(cipherKeyPath)) {
                        cipherKeyPath = cipherKeyFileResolver?.Invoke(options.KeySize);
                        // If no path was supplied, we bail out
                        if (cipherKeyPath == null) {
                            return new OpenFileResult(OpenFileStatus.Canceled, null, null, null);
                        }
                    }
                    cipherKey = System.IO.File.ReadAllBytes(cipherKeyPath);
                } else {
                    char[] password = passwordResolver?.Invoke();
                    // If no password was supplied, we bail out
                    if (password == null) {
                        return new OpenFileResult(OpenFileStatus.Canceled, null, null, null);
                    } else {
                        cipherKey = cipherEngine.GenerateKey(password);
                    }
                }

                // Try loading in the mac key if we need it 
                byte[] macKey = null;
                if (options.DigestType == DigestType.AESCMAC || options.DigestType == DigestType.HMACSHA256) {
                    string macKeyPath = GetPathForMacKeyFile(path);
                    if (!System.IO.File.Exists(macKeyPath)) {
                        macKeyPath = macKeyFileResolver?.Invoke();
                        // If no path was supplied, we bail out
                        if (macKeyPath == null) {
                            return new OpenFileResult(OpenFileStatus.Canceled, null, null, null);
                        }
                    }
                    macKey = System.IO.File.ReadAllBytes(macKeyPath);
                }

                // Verify signature
                SignatureEngine signatureEngine = new SignatureEngine(options.SignatureType, options.SignatureKeySize);
                if (!signatureEngine.Verify(cipher, Convert.FromBase64String(textFile.Base64Signature), Convert.FromBase64String(textFile.Base64SignatureKey))) {
                    return new OpenFileResult(OpenFileStatus.SignatureFailed, null, null, null);
                }

                // Decrypt cipher
                byte[] iv = textFile.Base64IV != null ? Convert.FromBase64String(textFile.Base64IV) : null;
                CipherDecryptResult decryptResult = cipherEngine.Decrypt(cipher, cipherKey, iv);
                if (decryptResult.Status == CipherDecryptStatus.MacFailed) {
                    return new OpenFileResult(OpenFileStatus.MacFailed, decryptResult.Exception, null, null);
                } else if (decryptResult.Status == CipherDecryptStatus.Failed) {
                    return new OpenFileResult(OpenFileStatus.Failed, decryptResult.Exception, null, null);
                }

                // Reverse the appending of the hash if needed
                byte[] messageDecrypted = decryptResult.Result;
                byte[] message = messageDecrypted;
                if (options.DigestType != DigestType.None) {
                    DigestEngine digestEngine = new DigestEngine(options.DigestType);

                    // We need to extract the hash from the cipher
                    int digestLength = digestEngine.GetDigestLength();
                    int messageLength = messageDecrypted.Length - digestLength;
                    message = messageDecrypted.Take(messageLength).ToArray();
                    byte[] digest = messageDecrypted.Skip(messageLength).ToArray();

                    // Compare saved and new computed digest
                    byte[] newDigest = digestEngine.Digest(message, macKey);
                    if (!DigestEngine.AreEqual(newDigest, digest)) {
                        return new OpenFileResult(OpenFileStatus.MacFailed, decryptResult.Exception, null, null);
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
                return new OpenFileResult(OpenFileStatus.Failed, e, null, null);
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
                return new CipherEngine(optionsAES.Type, optionsAES.Mode, optionsAES.Padding, options.KeyOption, options.KeySize);
            } else if (options is EncryptionOptionsRC4 optionsRC4) {
                return new CipherEngine(optionsRC4.Type, CipherMode.None, CipherPadding.None, options.KeyOption, options.KeySize);
            } else {
                return null;
            }
        }

        private static Encoding GetEncoding(TextEncoding encoding) {
            switch (encoding) {
                case TextEncoding.ASCII: return Encoding.ASCII;
                case TextEncoding.UTF8: return Encoding.UTF8;
                default: throw new ArgumentOutOfRangeException(nameof(encoding));
            }
        }

        private static string GetPathForCipherKeyFile(string basePath) {
            return Path.GetFileNameWithoutExtension(basePath) + CIPHER_KEY_FILE_EXTENSION;
        }

        private static string GetPathForMacKeyFile(string basePath) {
            return Path.GetFileNameWithoutExtension(basePath) + MAC_KEY_FILE_EXTENSION;
        }
    }
}
