using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using SecureTextEditor.Crypto.Cipher;
using SecureTextEditor.Crypto.Digest;
using SecureTextEditor.Crypto.Signature;
using SecureTextEditor.File;
using SecureTextEditor.File.Handler;
using SecureTextEditor.File.Options;
using SecureTextEditor.GUI.Config;
using SecureTextEditor.GUI.Dialog;
using SecureTextEditor.GUI.Editor;

// TODO: Finish xml docs
// FIXME: Refresh the whole ui when something changes so that the dependencies are easier to handle
// TODO: The signing should be treated as a different digest option

namespace SecureTextEditor.GUI {
    /// <summary>
    /// Interaction logic for the save window.
    /// </summary>
    public partial class SaveWindow : Window {
        private readonly IFileHandler m_FileHandler;
        private readonly ITextEditorControl m_TextEditorControl;
        private readonly ITextEditorTab m_TabToSave;
        private readonly bool m_CTSPaddingAvailable;
        private bool m_SaveInProgress;

        public SaveWindow(ITextEditorControl control, ITextEditorTab tab) {
            InitializeComponent();

            m_FileHandler = new SecureTextFileHandler();
            m_TextEditorControl = control;
            m_TabToSave = tab;
            m_CTSPaddingAvailable = m_TabToSave.Editor.Text.Length >= CipherEngine.BLOCK_SIZE;

            // Set up UI
            CipherTypeComboBox.ItemsSource = GetEnumValuesWithout<CipherType>();
            DigestTypeComboBox.ItemsSource = GetEnumValuesWithout<DigestType>();
            SignatureTypeComboBox.ItemsSource = GetEnumValuesWithout<SignatureType>();
            SignatureKeySizeComboBox.ItemsSource = SignatureEngine.DSA_ACCEPTED_KEYS;
            KeyOptionComboBox.ItemsSource = GetEnumValuesWithout<CipherKeyOption>();
            AESPaddingComboBox.ItemsSource = GetEnumValuesWithout<CipherPadding>();

            // Set default options
            EncryptionOptions options = tab.MetaData.FileMetaData.EncryptionOptions;
            CipherTypeComboBox.SelectedItem = options.CipherType;
            DigestTypeComboBox.SelectedItem = options.DigestType;
            SignatureTypeComboBox.SelectedItem = options.SignatureType;
            SignatureKeySizeComboBox.SelectedItem = options.SignatureKeySize;
            KeyOptionComboBox.SelectedItem = options.CipherKeyOption;

            EncryptionOptionsAES optionsAES = GetDefaultEncryptionOptions<EncryptionOptionsAES>(options, CipherType.AES);
            AESModeComboBox.SelectedItem = optionsAES.CipherMode;
            AESPaddingComboBox.SelectedItem = optionsAES.CipherPadding;

            // Set up events
            CipherTypeComboBox.SelectionChanged += (s, e) => {
                if (CipherTypeComboBox.SelectedItem is CipherType type) {
                    OnCipherTypeSelectionChanged(type);
                }
            };
            SignatureTypeComboBox.SelectionChanged += (s, e) => {
                if (SignatureTypeComboBox.SelectedItem is SignatureType type) {
                    OnSignatureTypeSelectionChanged(type);
                }
            };
            KeyOptionComboBox.SelectionChanged += (s, e) => {
                if (KeyOptionComboBox.SelectedItem is CipherKeyOption option) {
                    OnKeyOptionSelectionChanged(option);
                }
            };
            AESModeComboBox.SelectionChanged += (s, e) => {
                if (AESModeComboBox.SelectedItem is CipherMode mode) {
                    OnAESModeSelectionChanged(mode);
                }
            };
            AESPaddingComboBox.SelectionChanged += (s, e) => {
                if (AESPaddingComboBox.SelectedItem is CipherPadding padding) {
                    OnAESPaddingSelectionChanged(padding);
                }
            };
            PasswordTextBox.PasswordChanged += (s, e) => OnPasswordChanged(PasswordTextBox.Password);

            // Set up initial ui visibility
            OnCipherTypeSelectionChanged(options.CipherType);
            OnPasswordChanged("");
            OnAESPaddingSelectionChanged(optionsAES.CipherPadding);
            OnAESModeSelectionChanged(optionsAES.CipherMode);
            OnKeyOptionSelectionChanged(options.CipherKeyOption);

            // For some selections it is a littly hacky because of the weird dependency to the padding
            if (AESModeComboBox.Items.Contains(optionsAES.CipherMode)) {
                AESModeComboBox.SelectedItem = optionsAES.CipherMode;
            } else {
                AESPaddingComboBox.SelectedItem = CipherPadding.None;
                AESModeComboBox.SelectedItem = optionsAES.CipherMode;
            }
            KeySizeComboBox.SelectedItem = options.CipherKeySize;
            KeyOptionComboBox.SelectedItem = options.CipherKeyOption;

            OnSignatureTypeSelectionChanged(options.SignatureType);
            SignatureKeySizeComboBox.SelectedItem = options.SignatureKeySize;
        }

        private void CancelSave(object sender, RoutedEventArgs e) {
            // When canceling we can just close the window
            Close();
        }

        private async void Save(object sender, RoutedEventArgs e) {
            // Turn on the save indicator
            SavingIndicator.Visibility = Visibility.Visible;

            // Turn off interactability on cancel and save button
            CancelButton.IsEnabled = false;
            SaveButton.IsEnabled = false;
            
            // Do the actual save 
            m_SaveInProgress = true;
            TextEncoding encoding = m_TabToSave.MetaData.FileMetaData.Encoding;
            string text = m_TabToSave.Editor.Text;
            SecureString password = PasswordTextBox.SecurePassword;
            FileMetaData fileMetaData = await PerformSave(text, encoding, BuildEncryptionOptions(), password);
            // It is important that we clear out the password text box!
            PasswordTextBox.Clear();
            m_SaveInProgress = false;

            // Proceed only if the file got actually saved
            if (fileMetaData != null) {
                // This is a little hackey that we do it here but it works
                m_TextEditorControl.NotifyThatTabGotSaved(m_TabToSave);

                // Set new meta data for alreay existing tab and update its header
                m_TabToSave.MetaData = new TextEditorTabMetaData() {
                    FileMetaData = fileMetaData,
                    IsNew = false,
                    IsDirty = false
                };
                m_TabToSave.SetHeader(fileMetaData.FileName);

                // We can close the dialog when finished
                Close();
            }

            // Turn off the save indicator and turn on buttons again
            SavingIndicator.Visibility = Visibility.Hidden;
            CancelButton.IsEnabled = true;
            SaveButton.IsEnabled = true;
        }

        private async Task<FileMetaData> PerformSave(string text, TextEncoding encoding, EncryptionOptions options, SecureString password) {
            // Show dialog for saving a file
            SaveFileDialog dialog = new SaveFileDialog() {
                Title = "Save Secure Text File",
                AddExtension = true,
                Filter = FileFilters.STXT_FILE_FILTER
            };
            bool? saveFileResult = dialog.ShowDialog();
            // If no path for saving was selected we can bail out
            if (saveFileResult == false) {
                return null;
            }
            string path = dialog.FileName;

            SaveFileParameters parameters = new SaveFileParameters() {
                Path = path,
                Text = text,
                Encoding = encoding,
                EncryptionOptions = options,
                Password = password
            };
            SaveFileResult result = await m_FileHandler.SaveFileAsync(parameters);
            if (result.Status == SaveFileStatus.Success) {
                return result.FileMetaData;
            } else {
                DialogBox.Show(
                    Application.Current.MainWindow,
                    $"Failed to save the file:\n{path}!\n{result.Exception.GetType()}\n{result.Exception.Message}",
                    "Saving Failed",
                    DialogBoxButton.OK,
                    DialogBoxIcon.Error
                );
                return null;
            }
        }

        private void OnCipherTypeSelectionChanged(CipherType type) {
            AESOptions.Visibility = type == CipherType.AES ? Visibility.Visible : Visibility.Hidden;
            KeyOptionComboBox.ItemsSource = type == CipherType.AES ? GetEnumValuesWithout<CipherKeyOption>() : GetEnumValuesWithout(CipherKeyOption.PBEWithSCRYPT);
            KeyOptionComboBox.SelectedIndex = 0;
            KeySizeComboBox.ItemsSource = type == CipherType.AES ? CipherEngine.AES_ACCEPTED_KEYS : CipherEngine.RC4_ACCEPTED_KEYS;
            KeySizeComboBox.SelectedIndex = KeySizeComboBox.Items.Count - 1;
        }

        private void OnSignatureTypeSelectionChanged(SignatureType type) {
            if (type == SignatureType.None) {
                SignatureKeySizeComboBox.IsEnabled = false;
                DigestTypeComboBox.IsEnabled = true;
            } else {
                SignatureKeySizeComboBox.IsEnabled = true;
                SignatureKeySizeComboBox.ItemsSource = type == SignatureType.DSAWithSHA256 ? SignatureEngine.DSA_ACCEPTED_KEYS : SignatureEngine.ECDSA_ACCEPTED_KEYS;
                SignatureKeySizeComboBox.SelectedIndex = SignatureKeySizeComboBox.Items.Count - 1;
                DigestTypeComboBox.SelectedItem = DigestType.None;
                DigestTypeComboBox.IsEnabled = false;
            }
        }

        private void OnKeyOptionSelectionChanged(CipherKeyOption option) {
            KeySizeOption.Visibility = option == CipherKeyOption.Generate ? Visibility.Visible : Visibility.Hidden;
            PasswordOption.Visibility = option == CipherKeyOption.PBE || option == CipherKeyOption.PBEWithSCRYPT ? Visibility.Visible : Visibility.Hidden;
            SaveButton.IsEnabled = option == CipherKeyOption.Generate;
            PasswordTextBox.Clear();

            // Use special options for pbe and pbe with SCRYPT
            if (option == CipherKeyOption.PBE) {
                // PBE supports only CBC mode with PKCS7 padding
                AESPaddingComboBox.SelectedItem = CipherPadding.PKCS7;
                AESModeComboBox.SelectedItem = CipherMode.CBC;
            } else if (option == CipherKeyOption.PBEWithSCRYPT) {
                // PBEWithSCRYPT supports only GCM mode
                AESPaddingComboBox.SelectedItem = CipherPadding.None;
                AESModeComboBox.SelectedItem = CipherMode.GCM;
            }
            AESModeComboBox.IsEnabled = option == CipherKeyOption.Generate;
            AESPaddingComboBox.IsEnabled = option == CipherKeyOption.Generate;
        }

        private void OnPasswordChanged(string password) {
            if ((CipherKeyOption)KeyOptionComboBox.SelectedItem != CipherKeyOption.Generate) {
                SaveButton.IsEnabled = password != null && password != "";
            }
        }

        private void OnAESPaddingSelectionChanged(CipherPadding padding) {
            if (padding == CipherPadding.None) {
                // Only modes that have no padding should be available
                // and we need to check if CTS is available
                if (m_CTSPaddingAvailable) {
                    AESModeComboBox.ItemsSource = GetEnumValuesWithout(CipherMode.ECB, CipherMode.CBC, CipherMode.None);
                } else {
                    AESModeComboBox.ItemsSource = GetEnumValuesWithout(CipherMode.CTS, CipherMode.ECB, CipherMode.CBC, CipherMode.None);
                }
            } else {
                // Only modes that have a padding should be available
                AESModeComboBox.ItemsSource = GetEnumValuesWithout(CipherMode.CTS, CipherMode.CTR, CipherMode.CFB, CipherMode.OFB, CipherMode.GCM, CipherMode.CCM, CipherMode.None);
            }

            // When changing just select the first element
            AESModeComboBox.SelectedIndex = 0;
        }

        private void OnAESModeSelectionChanged(CipherMode mode) {
            if (mode == CipherMode.GCM || mode == CipherMode.CCM) {
                DigestTypeComboBox.SelectedItem = DigestType.None;
                DigestTypeComboBox.IsEnabled = false;
            } else {
                DigestTypeComboBox.IsEnabled = true;
            }
        }

        private EncryptionOptions BuildEncryptionOptions() {
            CipherType encryptionType = (CipherType)CipherTypeComboBox.SelectedItem;
            EncryptionOptions options;
            switch (encryptionType) {
                case CipherType.AES:
                    options = new EncryptionOptionsAES() {
                        CipherMode = (CipherMode)AESModeComboBox.SelectedItem,
                        CipherPadding = (CipherPadding)AESPaddingComboBox.SelectedItem
                    };
                    break;
                case CipherType.RC4:
                    options = new EncryptionOptionsRC4();
                    break;
                default: throw new InvalidOperationException();
            }

            options.DigestType = (DigestType)DigestTypeComboBox.SelectedItem;
            options.SignatureType = (SignatureType)SignatureTypeComboBox.SelectedItem;
            options.SignatureKeySize = (int)SignatureKeySizeComboBox.SelectedItem;
            options.CipherKeyOption = (CipherKeyOption)KeyOptionComboBox.SelectedItem;
            options.CipherKeySize = (int)KeySizeComboBox.SelectedItem;
            
            return options;
        }

        private IEnumerable<T> GetEnumValuesWithout<T>(params T[] without) {
            return Enum.GetValues(typeof(T)).Cast<T>().Except(without);
        }

        private T GetDefaultEncryptionOptions<T>(EncryptionOptions options, CipherType type) where T : EncryptionOptions {
            return (options as T) ?? (T)AppConfig.Config.DefaultEncryptionOptions[type];
        }

        protected override void OnClosing(CancelEventArgs e) {
            // We want to cancel the window closing when a save is in progress
            if (m_SaveInProgress) {
                e.Cancel = true;
            }
            base.OnClosing(e);
        }
    }
}