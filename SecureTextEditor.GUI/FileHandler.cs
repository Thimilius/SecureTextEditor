using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using SecureTextEditor.Core;
using SecureTextEditor.Core.Cipher;
using SecureTextEditor.Core.Digest;
using SecureTextEditor.Core.Options;

namespace SecureTextEditor.GUI {
    public static class FileHandler {
        public class File {
            public string Text { get; set; }
            public FileMetaData MetaData { get; set; }
        }

        public const string STXT_FILE_FILTER = "Secure Text File (" + SecureTextFile.FILE_EXTENSION + ")|*" + SecureTextFile.FILE_EXTENSION;
        public const string KEY_FILE_FILTER = "Key File (" + KeyFile.FILE_EXTENSION + ")|*" + KeyFile.FILE_EXTENSION;

        public static async Task<FileMetaData> SaveFileAsync(EncryptionOptions options, TextEncoding encoding, string text) {
            // Show dialog for saving a file
            SaveFileDialog dialog = new SaveFileDialog() {
                AddExtension = true,
                Filter = STXT_FILE_FILTER
            };
            bool? result = dialog.ShowDialog();
            // If no path for saving was selected we can bail out
            if (result == false) {
                return null;
            }

            string path = dialog.FileName;

            await Task.Run(() => {
                // Encrypt text and save file
                CipherEngine cipherEngine = GetCryptoEngine(options, encoding);
                byte[] key = cipherEngine.GenerateKey(options.KeySize);
                byte[] iv = cipherEngine.GenerateIV();
                byte[] cipher = cipherEngine.Encrypt(text, key, iv);

                // We compute the digest from the encrypted cipher
                DigestEngine digestEngine = new DigestEngine(options.DigestType);
                byte[] digest = digestEngine.Digest(cipher);

                SecureTextFile textFile = new SecureTextFile(options, encoding, Convert.ToBase64String(digest), Convert.ToBase64String(cipher));
                SecureTextFile.Save(textFile, path);

                // Save key file next to text file
                KeyFile keyFile = new KeyFile(Convert.ToBase64String(key), Convert.ToBase64String(iv));
                KeyFile.Save(keyFile, path + KeyFile.FILE_EXTENSION);
            });
            await Task.Delay(250);

            return new FileMetaData() {
                Encoding = encoding,
                EncryptionOptions = options,
                FileName = dialog.SafeFileName,
                FilePath = path,
                IsNew = false,
                IsDirty = false
            };
        }

        public static File OpenFile(string path, string fileName) {
            // TODO: Do error checking

            // Load file and decrypt with corresponding encoding
            SecureTextFile textFile = SecureTextFile.Load(path);

            // Try loading in the key file at the same location
            string keyPath = path + KeyFile.FILE_EXTENSION;
            KeyFile keyFile;
            if (System.IO.File.Exists(keyPath)) {
                keyFile = KeyFile.Load(keyPath);
            } else {
                DialogWindow.Show(
                    Application.Current.MainWindow,
                    "The file you want to open requires a key file to decrypt!",
                    "Key File Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                
                // Show dialog for opening a file
                var dialog = new OpenFileDialog {
                    Filter = KEY_FILE_FILTER
                };
                bool? result = dialog.ShowDialog();
                // If no file for opening was selected we can bail out
                if (result == false) {
                    return null;
                }

                keyFile = KeyFile.Load(dialog.FileName);
            }

            TextEncoding encoding = textFile.Encoding;
            EncryptionOptions options = textFile.EncryptionOptions;
            byte[] cipher = Convert.FromBase64String(textFile.Base64Cipher);

            // Compare saved and new computed digest
            DigestEngine digestEngine = new DigestEngine(options.DigestType);
            byte[] newDigest = digestEngine.Digest(cipher);
            byte[] oldDigest = Convert.FromBase64String(textFile.Base64Digest);
            if (!digestEngine.AreEqual(newDigest, oldDigest)) {
                DialogWindow.Show(
                    Application.Current.MainWindow,
                    "It appears the file got tampered with!\nIt can not be restored correctly!",
                    "File Tampered",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                return null;
            }

            // Decrypt cipher
            CipherEngine cipherEngine = GetCryptoEngine(options, encoding);
            byte[] key = Convert.FromBase64String(keyFile.Base64Key);
            byte[] iv = Convert.FromBase64String(keyFile.Base64IV);
            string text = cipherEngine.Decrypt(cipher, key, iv);

            return new File() {
                Text = text,
                MetaData = new FileMetaData() {
                    Encoding = encoding,
                    EncryptionOptions = options,
                    FileName = fileName,
                    FilePath = path,
                    IsNew = false,
                    IsDirty = false
                }
            };
        }

        private static CipherEngine GetCryptoEngine(EncryptionOptions options, TextEncoding encoding) {
            if (options is EncryptionOptionsAES optionsAES) {
                return new CipherEngine(optionsAES.CipherType, optionsAES.Mode, optionsAES.Padding, encoding);
            } else if (options is EncryptionOptionsRC4 optionsRC4) {
                return new CipherEngine(optionsRC4.CipherType, CipherMode.None, CipherPadding.None, encoding);
            } else {
                return null;
            }
        }
    }
}
