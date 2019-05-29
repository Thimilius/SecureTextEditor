using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SecureTextEditor.Crypto;
using SecureTextEditor.Crypto.Cipher;
using SecureTextEditor.Crypto.Digest;
using SecureTextEditor.File;
using SecureTextEditor.File.Options;
using SecureTextEditor.GUI.Editor;

namespace SecureTextEditor.GUI {
    // TODO: Improve hashing:
    // - Hash the normal message
    // - Append the hash to the message 
    // - Decrypt message+hash

    // TODO: Make part of file handler part of file project
    // TODO: Make a second file for the key used by macs
    // TODO: Use key size for usability when trying to load a key file with the wron size
    public static class FileHandler {
        /// <summary>
        /// Settings for serializing and deserializing the text file.
        /// </summary>
        private static readonly JsonSerializerSettings SERIALIZER_SETTINGS = new JsonSerializerSettings() {
            Formatting = Formatting.Indented,
            Converters = new List<JsonConverter>() { new StringEnumConverter() },
            TypeNameHandling = TypeNameHandling.Auto,
            NullValueHandling = NullValueHandling.Ignore
        };

        /// <summary>
        /// The extension used for the file.
        /// </summary>
        private const string KEY_FILE_EXTENSION = ".key";
        private const string STXT_FILE_FILTER = "Secure Text File (" + SecureTextFile.FILE_EXTENSION + ")|*" + SecureTextFile.FILE_EXTENSION;
        private const string KEY_FILE_FILTER = "Key File (" + KEY_FILE_EXTENSION + ")|*" + KEY_FILE_EXTENSION;

        public static async Task<FileMetaData> SaveFileAsync(EncryptionOptions options, TextEncoding encoding, string text) {
            // Show dialog for saving a file
            SaveFileDialog dialog = new SaveFileDialog() {
                Title = "Save Secure Text File",
                AddExtension = true,
                Filter = STXT_FILE_FILTER
            };
            bool? result = dialog.ShowDialog();
            // If no path for saving was selected we can bail out
            if (result == false) {
                return null;
            }

            string path = dialog.FileName;

            try { 
                await Task.Run(() => {
                    // Encrypt text and save file
                    CipherEngine cipherEngine = GetCryptoEngine(options);
                    byte[] key = cipherEngine.GenerateKey(options.KeySize);
                    byte[] iv = cipherEngine.GenerateIV();
                    byte[] cipher = cipherEngine.Encrypt(GetEncoding(encoding).GetBytes(text), key, iv);

                    // We compute the digest from the encrypted cipher
                    DigestEngine digestEngine = new DigestEngine(options.DigestType);
                    byte[] digest = digestEngine.Digest(cipher);

                    SecureTextFile textFile = new SecureTextFile(options, encoding, iv != null ? Convert.ToBase64String(iv) : null, Convert.ToBase64String(digest), Convert.ToBase64String(cipher));
                    SaveFile(path, textFile);

                    // Save key file next to text file
                    System.IO.File.WriteAllBytes(path + KEY_FILE_EXTENSION, key);
                });
                await Task.Delay(250);
            } catch {
                DialogWindow.Show(
                    Application.Current.MainWindow,
                    $"Failed to save the file:\n{path}!",
                    "Saving failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return null;
            }

            return new FileMetaData() {
                Encoding = encoding,
                EncryptionOptions = options,
                FileName = dialog.SafeFileName,
                FilePath = path,
                IsNew = false,
                IsDirty = false
            };
        }

        public static (string text, FileMetaData metaData) OpenFile(ITextEditorControl control, string path) {
            try {
                string fileName = Path.GetFileName(path);
                
                // Check if we need to show the open file dialog first
                if (path == null) {
                    // Show dialog for opening a file
                    var dialog = new OpenFileDialog {
                        Title = "Open Secure Text File",
                        Filter = STXT_FILE_FILTER
                    };
                    bool? result = dialog.ShowDialog();

                    path = dialog.FileName;
                    fileName = dialog.SafeFileName;

                    // If no file for opening was selected we can bail out
                    if (result == false || CheckFileAlreadyLoaded(control, path)) {
                        return (null, null);
                    }
                }

                // Load file and decrypt with corresponding encoding
                SecureTextFile textFile = LoadFile<SecureTextFile>(path);

                // Try loading in the key file at the same location
                byte[] key = null;
                string keyPath = path + KEY_FILE_EXTENSION;
                if (!System.IO.File.Exists(keyPath)) {
                    DialogWindow.Show(
                        Application.Current.MainWindow,
                        "The file you want to open requires a key file to decrypt!",
                        "Key File Required",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                    // Show dialog for opening a file
                    var dialog = new OpenFileDialog {
                        Title = "Open Key File",
                        Filter = KEY_FILE_FILTER
                    };
                    bool? result = dialog.ShowDialog();
                    // If no file for opening was selected we can bail out
                    if (result == false) {
                        return (null, null);
                    }

                    keyPath = dialog.FileName;
                }
                key = System.IO.File.ReadAllBytes(keyPath);

                TextEncoding encoding = textFile.Encoding;
                EncryptionOptions options = textFile.EncryptionOptions;
                byte[] cipher = Convert.FromBase64String(textFile.Base64Cipher);

                // Compare saved and new computed digest
                DigestEngine digestEngine = new DigestEngine(options.DigestType);
                byte[] newDigest = digestEngine.Digest(cipher);
                byte[] oldDigest = Convert.FromBase64String(textFile.Base64Digest);
                if (!DigestEngine.AreEqual(newDigest, oldDigest)) {
                    DialogWindow.Show(
                        Application.Current.MainWindow,
                        "It appears the file got tampered with!\nIt can not be restored correctly!",
                        "File Tampered",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );

                    return (null, null);
                }

                // Decrypt cipher
                CipherEngine cipherEngine = GetCryptoEngine(options);
                byte[] iv = Convert.FromBase64String(textFile.Base64IV);
                string text = GetEncoding(encoding).GetString(cipherEngine.Decrypt(cipher, key, iv));

                return (
                    text,
                    new FileMetaData() {
                        Encoding = encoding,
                        EncryptionOptions = options,
                        FileName = fileName,
                        FilePath = path,
                        IsNew = false,
                        IsDirty = false
                    }
                );
            } catch {
                DialogWindow.Show(
                    Application.Current.MainWindow,
                    $"Failed to open the file:\n{path}!",
                    "Opening failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return (null, null);
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

        private static bool CheckFileAlreadyLoaded(ITextEditorControl control, string path) {
            // We do not need to open the file if we already have it open
            // Instead we can just focus the corresponding tab
            if (path != null) {
                var tabs = control.Tabs.Where(t => t.FileMetaData.FilePath == path);
                if (tabs.Any()) {
                    control.FocusTab(tabs.First());
                    return true;
                }
            }

            return false;
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
