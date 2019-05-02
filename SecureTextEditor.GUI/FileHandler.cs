using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using SecureTextEditor.Core;
using SecureTextEditor.GUI.Editor;

namespace SecureTextEditor.GUI {
    public static class FileHandler {
        private const string FILE_FILTER = "Secure Text File (" + SecureTextFile.FILE_EXTENSION + ")|*" + SecureTextFile.FILE_EXTENSION;

        public static async Task SaveFileAsync(string text, TextEncoding textEncoding) {
            // Show dialog for saving a file
            SaveFileDialog dialog = new SaveFileDialog() {
                AddExtension = true,
                Filter = FILE_FILTER
            };
            bool? result = dialog.ShowDialog();
            // If no path for saving was selected we can bail out
            if (result == false) {
                return;
            }

            await Task.Run(() => {
                // Encrypt text and save file
                Encoding encoding = GetEncoding(textEncoding);
                CryptoPlaceholder crypto = new CryptoPlaceholder(encoding);
                string base64Cipher = crypto.Encrypt(text);
                SecureTextFile file = new SecureTextFile(textEncoding, base64Cipher);
                SecureTextFile.Save(file, dialog.FileName);
            });
            await Task.Delay(100);
        }

        public static string OpenFile(out TextEncoding textEncoding) {
            // TODO: Enable loading of normal text files

            // Show dialog for opening a file
            var dialog = new OpenFileDialog {
                Filter = FILE_FILTER
            };
            bool? result = dialog.ShowDialog();
            // If no file for opening was selected we can bail out
            if (result == false) {
                textEncoding = TextEncoding.UTF8;
                return null;
            }

            // TODO: Do error checking
            // Load file and decrypt with corresponding encoding
            var file = SecureTextFile.Load(dialog.FileName);
            textEncoding = file.Encoding;
            
            // TODO: Check encoding is valid
            var encoding = GetEncoding(textEncoding);
            var crpytoPlaceholder = new CryptoPlaceholder(encoding);
            return crpytoPlaceholder.Decrypt(file.Base64Cipher);
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
