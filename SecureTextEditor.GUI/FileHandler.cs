using Microsoft.Win32;
using SecureTextEditor.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureTextEditor.GUI {
    public static class FileHandler {
        private const string FILE_FILTER = "Secure Text File (" + SecureTextFile.FILE_EXTENSION + ")|*" + SecureTextFile.FILE_EXTENSION;

        public static async Task SaveFileAsync(string text, Encoding encoding) {
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
                CryptoPlaceholder crypto = new CryptoPlaceholder(encoding);
                string base64Cipher = crypto.Encrypt(text);
                SecureTextFile file = new SecureTextFile(encoding.CodePage, base64Cipher);
                SecureTextFile.Save(file, dialog.FileName);
            });
            await Task.Delay(100);
        }

        public static string OpenFile() {
            // Show dialog for opening a file
            var dialog = new OpenFileDialog {
                Filter = FILE_FILTER
            };
            bool? result = dialog.ShowDialog();
            // If no file for opening was selected we can bail out
            if (result == false) {
                return null;
            }

            // Load file and decrypt with corresponding encoding
            var file = SecureTextFile.Load(dialog.FileName);
            // TODO: Check encoding is valid
            var encoding = Encoding.GetEncoding(file.Encoding);
            var crpytoPlaceholder = new CryptoPlaceholder(encoding);
            return crpytoPlaceholder.Decrypt(file.Base64Cipher);
        }
    }
}
