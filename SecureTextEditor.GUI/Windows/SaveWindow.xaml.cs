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

namespace SecureTextEditor.GUI {
    /// <summary>
    /// Interaction logic for the save window.
    /// </summary>
    public partial class SaveWindow : Window {
        /// <summary>
        /// The file handler for saving files.
        /// </summary>
        private readonly SaveFileHandler m_SaveFileHandler;
        /// <summary>
        /// Holds a reference to the text editor control.
        /// </summary>
        private readonly ITextEditorControl m_TextEditorControl;
        /// <summary>
        /// Holds a reference to the text editor tab that need
        /// </summary>
        private readonly ITextEditorTab m_TabToSave;
        /// <summary>
        /// Flag to save whether or not a save is in progress.
        /// </summary>
        private bool m_SaveInProgress;

        /// <summary>
        /// Creates a new save window for a tab.
        /// </summary>
        /// <param name="control">The text editor control</param>
        /// <param name="tab">The tab to save</param>
        public SaveWindow(ITextEditorControl control, ITextEditorTab tab) {
            InitializeComponent();

            m_SaveFileHandler = new SaveFileHandler();
            m_TextEditorControl = control;
            m_TabToSave = tab;

            SetUpUI(tab.MetaData.FileMetaData.EncryptionOptions);
        }

        /// <summary>
        /// Callback that gets executed when the cancel button gets clicked.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event parameters</param>
        private void CancelSave(object sender, RoutedEventArgs e) {
            // When canceling we can just close the window
            Close();
        }

        /// <summary>
        /// Callback that gets executed when the save button gets clicked.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event parameters</param>
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
            SecureString pbePassword = PBEPasswordTextBox.SecurePassword;
            FileMetaData fileMetaData = await PerformSave(text, encoding, BuildEncryptionOptions(), pbePassword);
            // It is important that we clear out the password text box!
            PBEPasswordTextBox.Clear();
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

        /// <summary>
        /// Performs the actual save operation asynchronously.
        /// </summary>
        /// <param name="text">The text to save</param>
        /// <param name="encoding">The text encoding to use</param>
        /// <param name="options">The encryption options to use</param>
        /// <param name="pbePassword">The password used in PBE</param>
        /// <returns>The file meta data for the saved file</returns>
        private async Task<FileMetaData> PerformSave(string text, TextEncoding encoding, EncryptionOptions options, SecureString pbePassword) {
            // Check if we need to prompt the user for the signature key storage password
            SecureString keyStoragePassword = null;
            if (options.SignatureType != SignatureType.None) {
                PasswordWindow window = new PasswordWindow(this, "You need to provide a password for the signature key storage!");
                bool? result = window.ShowDialog();

                if (result.Value) {
                    keyStoragePassword = window.Password;
                    window.Clear();
                } else {
                    return null;
                }
            }

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
                KeyStoragePath = AppConfig.Config.KeyStoragePath,
                KeyStoragePassword = keyStoragePassword,
                PBEPassword = pbePassword,
            };
            SaveFileResult saveResult = await m_SaveFileHandler.SaveFileAsync(parameters);

            // Clear out passwords
            pbePassword.Clear();
            keyStoragePassword.Clear();

            // Handle save file status
            switch (saveResult.Status) {
                case SaveFileStatus.Success:
                    return saveResult.FileMetaData;
                case SaveFileStatus.KeyStoragePasswordWrong:
                    DialogBox.Show(
                        Application.Current.MainWindow,
                        $"The provided key storage password is wrong!",
                        "Key Storage Password Wrong",
                        DialogBoxButton.OK,
                        DialogBoxIcon.Error
                    );
                    return null;
                case SaveFileStatus.Failed:
                    DialogBox.Show(
                        Application.Current.MainWindow,
                        $"Failed to save the file:\n{path}!\n{saveResult.Exception.GetType()}\n{saveResult.Exception.Message}",
                        "Saving Failed",
                        DialogBoxButton.OK,
                        DialogBoxIcon.Error
                    );
                    return null;
                default: throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Callback that gets executed when the cipher type changes.
        /// </summary>
        /// <param name="type">The selected cipher type</param>
        private void OnCipherTypeSelectionChanged(CipherType type) {
            // Set key sizes and cipher key options
            AESOptions.Visibility = type == CipherType.AES ? Visibility.Visible : Visibility.Hidden;
            CipherKeyOptionComboBox.ItemsSource = type == CipherType.AES ? GetEnumValuesWithout<CipherKeyOption>() : GetEnumValuesWithout(CipherKeyOption.PBEWithSCRYPT);
            CipherKeyOptionComboBox.SelectedIndex = 0;
            CipherKeySizeComboBox.ItemsSource = type == CipherType.AES ? CipherEngine.AES_ACCEPTED_KEYS : CipherEngine.RC4_ACCEPTED_KEYS;
            CipherKeySizeComboBox.SelectedIndex = CipherKeySizeComboBox.Items.Count - 1;
        }

        /// <summary>
        /// Callback that gets executed when the signature type changes.
        /// </summary>
        /// <param name="type">The selected signature type</param>
        private void OnSignatureTypeSelectionChanged(SignatureType type) {
            // Only allow digest option if we are not using a signature
            if (type == SignatureType.None) {
                SignatureKeySizeComboBox.IsEnabled = false;
                OnAESModeSelectionChanged((CipherMode)AESModeComboBox.SelectedItem);
            } else {
                // Set appropriate key sizes
                SignatureKeySizeComboBox.IsEnabled = true;
                SignatureKeySizeComboBox.ItemsSource = type == SignatureType.DSAWithSHA256 ? SignatureEngine.DSA_ACCEPTED_KEYS : SignatureEngine.ECDSA_ACCEPTED_KEYS;
                SignatureKeySizeComboBox.SelectedIndex = SignatureKeySizeComboBox.Items.Count - 1;
                DigestTypeComboBox.SelectedItem = DigestType.None;
                DigestTypeComboBox.IsEnabled = false;
            }
        }

        /// <summary>
        /// Callback that gets exectuted when the cipher key option changes
        /// </summary>
        /// <param name="option">The selected cipher key option</param>
        private void OnKeyOptionSelectionChanged(CipherKeyOption option) {
            KeySizeOption.Visibility = option == CipherKeyOption.Generate ? Visibility.Visible : Visibility.Hidden;
            PasswordOption.Visibility = option == CipherKeyOption.PBE || option == CipherKeyOption.PBEWithSCRYPT ? Visibility.Visible : Visibility.Hidden;
            SaveButton.IsEnabled = option == CipherKeyOption.Generate;
            PBEPasswordTextBox.Clear();

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

        /// <summary>
        /// Callback that gets executed when the password changes.
        /// </summary>
        /// <param name="password">The new password</param>
        private void OnPasswordChanged(SecureString password) {
            if ((CipherKeyOption)CipherKeyOptionComboBox.SelectedItem != CipherKeyOption.Generate) {
                SaveButton.IsEnabled = password != null && password.Length != 0;
            }
        }

        /// <summary>
        /// Callback that gets executed when the cipher mode changes.
        /// </summary>
        /// <param name="mode">The selected cipher mode</param>
        private void OnAESModeSelectionChanged(CipherMode mode) {
            // Only allow digest option if we are not using a signature
            if ((SignatureType)SignatureTypeComboBox.SelectedItem == SignatureType.None) {
                // GCM and CCM have built in integrity
                if (mode == CipherMode.GCM || mode == CipherMode.CCM) {
                    DigestTypeComboBox.SelectedItem = DigestType.None;
                    DigestTypeComboBox.IsEnabled = false;
                } else {
                    DigestTypeComboBox.IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// Callback that gets executed when the cipher padding changes.
        /// </summary>
        /// <param name="padding">The selected cipher padding</param>
        private void OnAESPaddingSelectionChanged(CipherPadding padding) {
            if (padding == CipherPadding.None) {
                // Only modes that have no padding should be available
                // and we need to check if CTS is available
                if (CipherEngine.IsCTSPaddingPossible(m_TabToSave.Editor.Text)) {
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

        /// <summary>
        /// Sets up the ui based on given encryption options.
        /// </summary>
        /// <param name="options">The encryption options to set up</param>
        private void SetUpUI(EncryptionOptions options) {
            // Set up UI
            CipherTypeComboBox.ItemsSource = GetEnumValuesWithout<CipherType>();
            DigestTypeComboBox.ItemsSource = GetEnumValuesWithout<DigestType>();
            SignatureTypeComboBox.ItemsSource = GetEnumValuesWithout<SignatureType>();
            SignatureKeySizeComboBox.ItemsSource = SignatureEngine.DSA_ACCEPTED_KEYS;
            CipherKeyOptionComboBox.ItemsSource = GetEnumValuesWithout<CipherKeyOption>();
            AESPaddingComboBox.ItemsSource = GetEnumValuesWithout<CipherPadding>();

            // Set default options
            CipherTypeComboBox.SelectedItem = options.CipherType;
            DigestTypeComboBox.SelectedItem = options.DigestType;
            SignatureTypeComboBox.SelectedItem = options.SignatureType;
            SignatureKeySizeComboBox.SelectedItem = options.SignatureKeySize;
            CipherKeyOptionComboBox.SelectedItem = options.CipherKeyOption;

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
            CipherKeyOptionComboBox.SelectionChanged += (s, e) => {
                if (CipherKeyOptionComboBox.SelectedItem is CipherKeyOption option) {
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
            PBEPasswordTextBox.PasswordChanged += (s, e) => OnPasswordChanged(PBEPasswordTextBox.SecurePassword);

            // Set up initial ui visibility
            OnCipherTypeSelectionChanged(options.CipherType);
            OnPasswordChanged(null);
            OnAESPaddingSelectionChanged(optionsAES.CipherPadding);
            OnAESModeSelectionChanged(optionsAES.CipherMode);
            OnKeyOptionSelectionChanged(options.CipherKeyOption);

            // We need to reset the key sizes and some modes because of the dependencies
            if (AESModeComboBox.Items.Contains(optionsAES.CipherMode)) {
                AESModeComboBox.SelectedItem = optionsAES.CipherMode;
            } else {
                AESPaddingComboBox.SelectedItem = CipherPadding.None;
                AESModeComboBox.SelectedItem = optionsAES.CipherMode;
            }
            CipherKeySizeComboBox.SelectedItem = options.CipherKeySize;
            CipherKeyOptionComboBox.SelectedItem = options.CipherKeyOption;

            OnSignatureTypeSelectionChanged(options.SignatureType);
            SignatureKeySizeComboBox.SelectedItem = options.SignatureKeySize;
        }

        /// <summary>
        /// Builds the encryption options from the current selected configuration.
        /// </summary>
        /// <returns>The built encryption options</returns>
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
            options.CipherKeyOption = (CipherKeyOption)CipherKeyOptionComboBox.SelectedItem;
            options.CipherKeySize = (int)CipherKeySizeComboBox.SelectedItem;
            
            return options;
        }

        /// <summary>
        /// Gets the values of an enums without a given set.
        /// </summary>
        /// <typeparam name="T">The enum to get the values from</typeparam>
        /// <param name="without">The set of values that should not be included</param>
        /// <returns>The list of enum values</returns>
        private IEnumerable<T> GetEnumValuesWithout<T>(params T[] without) {
            return Enum.GetValues(typeof(T)).Cast<T>().Except(without);
        }

        /// <summary>
        /// Gets the default encryption options for a specific cipher type.
        /// </summary>
        /// <typeparam name="T">The specific encryption options type</typeparam>
        /// <param name="options">The base encryption options</param>
        /// <param name="type">The cipher type</param>
        /// <returns>The default encryption options</returns>
        private T GetDefaultEncryptionOptions<T>(EncryptionOptions options, CipherType type) where T : EncryptionOptions {
            return (options as T) ?? (T)AppConfig.Config.DefaultEncryptionOptions[type];
        }

        /// <summary>
        /// Callback when the window gets closed.
        /// </summary>
        /// <param name="e">The event parameters</param>
        protected override void OnClosing(CancelEventArgs e) {
            // We want to cancel the window closing when a save is in progress
            if (m_SaveInProgress) {
                e.Cancel = true;
            }
            base.OnClosing(e);
        }
    }
}