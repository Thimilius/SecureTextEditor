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

        public static async Task<FileMetaData> SaveFileAsync(string text, CipherMode mode, CipherPadding padding, TextEncoding textEncoding) {
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
                Encoding encoding = GetEncoding(textEncoding);
                CryptoEngine crypto = new CryptoEngine(mode, padding, encoding);
                string base64Cipher = crypto.Encrypt(text);
                SecureTextFile file = new SecureTextFile(textEncoding, mode, padding, base64Cipher);
                SecureTextFile.Save(file, dialog.FileName);
            });
            await Task.Delay(250);

            return new FileMetaData() {
                Encoding = textEncoding,
                FileName = dialog.SafeFileName,
                FilePath = dialog.FileName,
                IsNew = false,
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
            // TODO: Check if we have the file already open
            // TODO: Do error checking

            // Load file and decrypt with corresponding encoding
            var file = SecureTextFile.Load(path);
            var textEncoding = file.Encoding;
            
            var encoding = GetEncoding(textEncoding);
            var crpyto = new CryptoEngine(file.Mode, file.Padding, encoding);
            string text = crpyto.Decrypt(file.Base64Cipher);

            return new File() {
                Text = text,
                MetaData = new FileMetaData() {
                    Encoding = textEncoding,
                    FileName = fileName,
                    FilePath = path,
                    IsNew = false
                }
            };
        }

        private static Encoding GetEncoding(TextEncoding encoding) {
            switch (encoding) {
                case TextEncoding.ASCII: return Encoding.ASCII;
                case TextEncoding.UTF8: return Encoding.UTF8;
                default: throw new System.Exception();
            }
        }
    }
}
