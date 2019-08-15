using System;
using System.IO;
using System.Linq;
using System.Security;
using Newtonsoft.Json;
using SecureTextEditor.Crypto;
using SecureTextEditor.Crypto.Cipher;
using SecureTextEditor.Crypto.Digest;
using SecureTextEditor.Crypto.Signature;
using SecureTextEditor.File.Options;

namespace SecureTextEditor.File.Handler {
    /// <summary>
    /// The file handler for opening a file.
    /// </summary>
    public class OpenFileHandler : FileHandler {
        /// <summary>
        /// Opens a file with given parameters.
        /// </summary>
        /// <param name="parameters">The open file parameters to use</param>
        /// <returns>The result of the open operation</returns>
        public OpenFileResult OpenFile(OpenFileParameters parameters) {
            try {
                string path = parameters.Path;
                string fileName = Path.GetFileName(path);

                // Load file and decrypt with corresponding encoding
                SecureTextFile textFile = LoadSecureTextFile(path);
                TextEncoding encoding = textFile.Encoding;
                EncryptionOptions options = textFile.EncryptionOptions;
                byte[] cipher = Convert.FromBase64String(textFile.Base64Cipher);
                byte[] iv = textFile.Base64IVOrSalt != null ? Convert.FromBase64String(textFile.Base64IVOrSalt) : null;

                CipherEngine cipherEngine = CreateCryptoEngine(options);

                // Get the key from a file or get it from a password in case of password based encryption
                byte[] cipherKey = null;
                if (options.CipherKeyOption == CipherKeyOption.Generate) {
                    // Try loading in the key file at the same location
                    string cipherKeyPath = ConstructPathForCipherKeyFile(path);
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
                        password.Process(chars => cipherKey = cipherEngine.GenerateKey(chars, iv));
                    }
                }

                // Try loading in the mac key if we need it
                byte[] macKey = null;
                if (options.DigestType == DigestType.AESCMAC || options.DigestType == DigestType.HMACSHA256) {
                    string macKeyPath = ConstructPathForMacKeyFile(path);
                    if (!System.IO.File.Exists(macKeyPath)) {
                        macKeyPath = parameters.MacKeyFileResolver?.Invoke();
                        // If no path was supplied, we bail out
                        if (macKeyPath == null) {
                            return new OpenFileResult(OpenFileStatus.Canceled);
                        }
                    }
                    macKey = System.IO.File.ReadAllBytes(macKeyPath);
                }

                // Verify signature
                if (options.SignatureType != SignatureType.None) {
                    SignatureEngine signatureEngine = new SignatureEngine(options.SignatureType, options.SignatureKeySize);
                    if (!signatureEngine.Verify(cipher, Convert.FromBase64String(textFile.Base64Signature), Convert.FromBase64String(textFile.Base64SignaturePublicKey))) {
                        return new OpenFileResult(OpenFileStatus.SignatureFailed);
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

        /// <summary>
        /// Loads a secure text file from a givenpath
        /// </summary>
        /// <param name="path">The path to the secure text file</param>
        /// <returns>The loaded secure text file</returns>
        private static SecureTextFile LoadSecureTextFile(string path) {
            string json = System.IO.File.ReadAllText(path);
            return JsonConvert.DeserializeObject<SecureTextFile>(json, SERIALIZER_SETTINGS);
        }
    }
}