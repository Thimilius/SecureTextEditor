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
using SecureTextEditor.File.Options;

namespace SecureTextEditor.File.Handler {
    // TODO: Use key size for usability when trying to load a key file with the wrong size
    // TODO: Key files should maybe not have the ".stxt" extension included
    public static class FileHandler {
        public enum OpenFileStatus {
            Success,
            Canceled,
            MacFailed,
            Failed
        }

        public class OpenFileResult {
            public OpenFileStatus Status { get; }
            public Exception Exception { get; }
            public FileMetaData FileMetaData { get; }
            public string Text { get; }

            public OpenFileResult(OpenFileStatus status, Exception exception, FileMetaData fileMetaData, string text) {
                Status = status;
                Exception = exception;
                FileMetaData = fileMetaData;
                Text = text;
            }
        }

        public enum SaveFileStatus {
            Success,
            Failed
        }

        public class SaveFileResult {
            public SaveFileStatus Status { get; }
            public Exception Exception { get; }
            public FileMetaData FileMetaData { get; }

            public SaveFileResult(SaveFileStatus status, Exception exception, FileMetaData fileMetaData) {
                Status = status;
                Exception = exception;
                FileMetaData = fileMetaData;
            }
        }

        /// <summary>
        /// Settings for serializing and deserializing the text file.
        /// </summary>
        private static readonly JsonSerializerSettings SERIALIZER_SETTINGS = new JsonSerializerSettings() {
            Formatting = Formatting.Indented,
            Converters = new List<JsonConverter>() { new StringEnumConverter() },
            TypeNameHandling = TypeNameHandling.Auto,
            NullValueHandling = NullValueHandling.Ignore
        };

        // FIXME: Where should these constants actually be? Maybe put them into their own FileFilter class
        public const string STXT_FILE_FILTER = "Secure Text File (" + STXT_FILE_EXTENSION + ")|*" + STXT_FILE_EXTENSION;
        public const string CIPHER_KEY_FILE_FILTER = "Cipher Key File (" + CIPHER_KEY_FILE_EXTENSION + ")|*" + CIPHER_KEY_FILE_EXTENSION;
        public const string MAC_KEY_FILE_FILTER = "Mac Key File (" + MAC_KEY_FILE_EXTENSION + ")|*" + MAC_KEY_FILE_EXTENSION;

        /// <summary>
        /// The extension used for the file.
        /// </summary>
        public const string STXT_FILE_EXTENSION = ".stxt";
        /// <summary>
        /// The extension used for the cipher key file.
        /// </summary>
        private const string CIPHER_KEY_FILE_EXTENSION = ".key";
        /// <summary>
        /// The extension used for the mac key file.
        /// </summary>
        private const string MAC_KEY_FILE_EXTENSION = ".mackey";

        public static async Task<SaveFileResult> SaveFileAsync(string path, EncryptionOptions options, TextEncoding encoding, string text) {
            try {
                string fileName = Path.GetFileName(path);

                await Task.Run(() => {
                    byte[] encodedText = GetEncoding(encoding).GetBytes(text);

                    // We compute the digest from message
                    DigestEngine digestEngine = new DigestEngine(options.DigestType);
                    byte[] macKey = digestEngine.GenerateKey();
                    byte[] digest = digestEngine.Digest(encodedText, macKey);

                    // Append the digest to the text
                    byte[] full = new byte[encodedText.Length + digest.Length];
                    Buffer.BlockCopy(encodedText, 0, full, 0, encodedText.Length);
                    Buffer.BlockCopy(digest, 0, full, encodedText.Length, digest.Length);

                    // Encrypt text and save file
                    CipherEngine cipherEngine = GetCryptoEngine(options);
                    byte[] cipherKey = cipherEngine.GenerateKey(options.KeySize);
                    byte[] iv = cipherEngine.GenerateIV();
                    byte[] cipher = cipherEngine.Encrypt(full, cipherKey, iv);

                    SecureTextFile textFile = new SecureTextFile(options, encoding, iv != null ? Convert.ToBase64String(iv) : null, Convert.ToBase64String(cipher));
                    SaveFile(path, textFile);

                    // Save cipher key into file next to the text file
                    string cipherKeyPath = path + CIPHER_KEY_FILE_EXTENSION;
                    System.IO.File.WriteAllBytes(cipherKeyPath, cipherKey);

                    // If we have a mac key to save, save it to a seperate file as well
                    if (macKey != null) {
                        string macKeyPath = path + MAC_KEY_FILE_EXTENSION;
                        System.IO.File.WriteAllBytes(macKeyPath, macKey);
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

        public static OpenFileResult OpenFile(string path, Func<string> cipherKeyFileResolver, Func<string> macKeyFileResolver) {
            try {
                string fileName = Path.GetFileName(path);

                // Load file and decrypt with corresponding encoding
                SecureTextFile textFile = LoadFile<SecureTextFile>(path);
                TextEncoding encoding = textFile.Encoding;
                EncryptionOptions options = textFile.EncryptionOptions;

                // Try loading in the key file at the same location
                byte[] cipherKey = null;
                string cipherKeyPath = path + CIPHER_KEY_FILE_EXTENSION;
                if (!System.IO.File.Exists(cipherKeyPath)) {
                    cipherKeyPath = cipherKeyFileResolver?.Invoke();
                    // If no path was supplied, we bail out
                    if (cipherKeyPath == null) {
                        return new OpenFileResult(OpenFileStatus.Canceled, null, null, null);
                    }
                }
                cipherKey = System.IO.File.ReadAllBytes(cipherKeyPath);

                // Try loading in the mac key if we need it 
                byte[] macKey = null;
                if (options.DigestType != DigestType.SHA256) {
                    string macKeyPath = path + MAC_KEY_FILE_EXTENSION;
                    if (!System.IO.File.Exists(macKeyPath)) {
                        macKeyPath = macKeyFileResolver?.Invoke();
                        // If no path was supplied, we bail out
                        if (macKeyPath == null) {
                            return new OpenFileResult(OpenFileStatus.Canceled, null, null, null);
                        }
                    }
                    macKey = System.IO.File.ReadAllBytes(macKeyPath);
                }

                // Decrypt cipher
                CipherEngine cipherEngine = GetCryptoEngine(options);
                byte[] iv = textFile.Base64IV != null ? Convert.FromBase64String(textFile.Base64IV) : null;
                CipherEngine.DecryptResult decryptResult = cipherEngine.Decrypt(Convert.FromBase64String(textFile.Base64Cipher), cipherKey, iv);
                if (decryptResult.Status == CipherEngine.DecryptStatus.MacFailed) {
                    return new OpenFileResult(OpenFileStatus.MacFailed, decryptResult.Exception, null, null);
                } else if (decryptResult.Status == CipherEngine.DecryptStatus.Failed) {
                    return new OpenFileResult(OpenFileStatus.Failed, decryptResult.Exception, null, null);
                }

                byte[] full = decryptResult.Result;

                DigestEngine digestEngine = new DigestEngine(options.DigestType);

                // We need to extract the hash from the cipher
                int digestLength = digestEngine.GetDigestLength();
                int messageLength = full.Length - digestLength;
                byte[] message = full.Take(messageLength).ToArray();
                byte[] digest = full.Skip(messageLength).ToArray();

                // Compare saved and new computed digest
                byte[] newDigest = digestEngine.Digest(message, macKey);
                if (!DigestEngine.AreEqual(newDigest, digest)) {
                    return new OpenFileResult(OpenFileStatus.MacFailed, decryptResult.Exception, null, null);
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

        private static void SaveFile<T>(string path, T file) {
            string json = JsonConvert.SerializeObject(file, SERIALIZER_SETTINGS);
            System.IO.File.WriteAllText(path, json);
        }

        private static T LoadFile<T>(string path) {
            string json = System.IO.File.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(json, SERIALIZER_SETTINGS);
        }

        private static CipherEngine GetCryptoEngine(EncryptionOptions options) {
            if (options is EncryptionOptionsAES optionsAES) {
                return new CipherEngine(optionsAES.CipherType, optionsAES.Mode, optionsAES.Padding);
            } else if (options is EncryptionOptionsRC4 optionsRC4) {
                return new CipherEngine(optionsRC4.CipherType, CipherMode.None, CipherPadding.None);
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
    }
}
