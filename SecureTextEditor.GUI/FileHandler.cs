using System;
using System.Threading.Tasks;
using Microsoft.Win32;
using SecureTextEditor.Core;

namespace SecureTextEditor.GUI {
    public static class FileHandler {
        public class File {
            public string Text { get; set; }
            public FileMetaData MetaData { get; set; }
        }

        private const string FILE_FILTER = "Secure Text File (" + SecureTextFile.FILE_EXTENSION + ")|*" + SecureTextFile.FILE_EXTENSION;

        public static async Task<FileMetaData> SaveFileAsync(EncryptionOptions options, TextEncoding encoding, string text) {
            // Show dialog for saving a file
            SaveFileDialog dialog = new SaveFileDialog() {
                AddExtension = true,
                Filter = FILE_FILTER
            };
            bool? result = dialog.ShowDialog();
            // If no path for saving was selected we can bail out
            if (result == false) {
                return null;
            }

            string path = dialog.FileName;

            await Task.Run(() => {
                // Encrypt text and save file
                CryptoEngine crypto = new CryptoEngine(options.CipherType, options.CipherBlockMode, options.CipherBlockPadding, encoding);
                byte[] key = crypto.GenerateKey(options.CipherKeySize);
                byte[] iv = crypto.GenerateIV();
                byte[] cipher = crypto.Encrypt(text, key, iv);
                SecureTextFile textFile = new SecureTextFile(options, encoding, Convert.ToBase64String(cipher));
                SecureTextFile.Save(textFile, path);

                // HACK: Hardcoded path to key file
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

        public static File OpenFile() {
            // Show dialog for opening a file
            var dialog = new OpenFileDialog {
                Filter = FILE_FILTER
            };
            bool? result = dialog.ShowDialog();
            // If no file for opening was selected we can bail out
            if (result == false) {
                return null;
            }

            return OpenFile(dialog.FileName, dialog.SafeFileName);
        }

        public static File OpenFile(string path, string fileName) {
            // TODO: Enable loading of normal text files
            // TODO: Do error checking

            // Load file and decrypt with corresponding encoding
            SecureTextFile textFile = SecureTextFile.Load(path);

            // HACK: Hardcoded path to key file
            KeyFile keyFile = KeyFile.Load(path + KeyFile.FILE_EXTENSION);

            TextEncoding encoding = textFile.Encoding;
            EncryptionOptions options = textFile.EncryptionOptions;

            CryptoEngine crpyto = new CryptoEngine(options.CipherType, options.CipherBlockMode, options.CipherBlockPadding, encoding);
            byte[] cipher = Convert.FromBase64String(textFile.Base64Cipher);
            byte[] key = Convert.FromBase64String(keyFile.Base64Key);
            byte[] iv = Convert.FromBase64String(keyFile.Base64IV);
            string text = crpyto.Decrypt(cipher, key, iv);

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
    }
}
