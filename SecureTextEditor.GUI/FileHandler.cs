using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using SecureTextEditor.Core;
using SecureTextEditor.GUI.Editor;

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

            await Task.Run(() => {
                // Encrypt text and save file
                CryptoEngine crypto = new CryptoEngine(options.BlockMode, options.BlockPadding, encoding);
                string base64Cipher = crypto.Encrypt(text);
                SecureTextFile file = new SecureTextFile(options, encoding, base64Cipher);
                SecureTextFile.Save(file, dialog.FileName);
            });
            await Task.Delay(250);

            return new FileMetaData() {
                Encoding = encoding,
                EncryptionOptions = options,
                FileName = dialog.SafeFileName,
                FilePath = dialog.FileName,
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
            var file = SecureTextFile.Load(path);
            var encoding = file.Encoding;
            
            var crpyto = new CryptoEngine(file.EncryptionOptions.BlockMode, file.EncryptionOptions.BlockPadding, encoding);
            string text = crpyto.Decrypt(file.Base64Cipher);

            return new File() {
                Text = text,
                MetaData = new FileMetaData() {
                    Encoding = encoding,
                    EncryptionOptions = file.EncryptionOptions,
                    FileName = fileName,
                    FilePath = path,
                    IsNew = false,
                    IsDirty = false
                }
            };
        }
    }
}
