using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using SecureTextEditor.Crypto;
using SecureTextEditor.Crypto.Cipher;
using SecureTextEditor.Crypto.Digest;
using SecureTextEditor.File;
using SecureTextEditor.File.Handler;
using SecureTextEditor.File.Options;
using SecureTextEditor.GUI.Config;
using SecureTextEditor.GUI.Editor;

namespace SecureTextEditor.GUI {
    /// <summary>
    /// Interaction logic for SaveWindow.xaml
    /// </summary>
    public partial class SaveWindow : Window {
        private readonly ITextEditorControl m_TextEditorControl;
        private readonly ITextEditorTab m_TabToSave;
        private readonly bool m_CTSPaddingAvailable;
        private bool m_SaveInProgress;

        public SaveWindow(ITextEditorControl control, ITextEditorTab tab) {
            InitializeComponent();

            m_TextEditorControl = control;
            m_TabToSave = tab;
            m_CTSPaddingAvailable = m_TabToSave.Editor.Text.Length >= CipherEngine.BLOCK_SIZE;

            // Set up UI
            DigestTypeComboBox.ItemsSource = GetEnumValuesWithout<DigestType>();
            EncryptionTypeComboBox.ItemsSource = GetEnumValuesWithout<EncryptionType>();
            AESKeySizeComboBox.ItemsSource = CipherEngine.AES_ACCEPTED_KEYS;
            AESPaddingComboBox.ItemsSource = GetEnumValuesWithout<CipherPadding>();
            RC4KeySizeComboBox.ItemsSource = CipherEngine.RC4_ACCEPTED_KEYS;

            // Set default options
            EncryptionOptions options = tab.MetaData.FileMetaData.EncryptionOptions;
            EncryptionTypeComboBox.SelectedItem = options.Type;
            AESKeySizeComboBox.SelectedItem = options.KeySize;
            RC4KeySizeComboBox.SelectedItem = options.KeySize;
            DigestTypeComboBox.SelectedItem = options.DigestType;

            EncryptionOptionsAES optionsAES = GetDefaultEncryptionOptions<EncryptionOptionsAES>(options, EncryptionType.AES);
            AESModeComboBox.SelectedItem = optionsAES.Mode;
            AESPaddingComboBox.SelectedItem = optionsAES.Padding;

            // Set up events
            EncryptionTypeComboBox.SelectionChanged += (s, e) => OnSecurityTypeSelectionChanged((EncryptionType)EncryptionTypeComboBox.SelectedItem);
            AESPaddingComboBox.SelectionChanged += (s, e) => OnAESPaddingSelectionChanged((CipherPadding)AESPaddingComboBox.SelectedItem);

            // Set up initial ui visibility
            OnSecurityTypeSelectionChanged(options.Type);
            OnAESPaddingSelectionChanged(optionsAES.Padding);

            // The selection of the mode is a littly hacky because of the weird dependency to the padding
            if (AESModeComboBox.Items.Contains(optionsAES.Mode)) {
                AESModeComboBox.SelectedItem = optionsAES.Mode;
            } else {
                AESPaddingComboBox.SelectedItem = CipherPadding.None;
                AESModeComboBox.SelectedItem = optionsAES.Mode;
            }
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
            FileMetaData fileMetaData = await PerformSave(BuildEncryptionOptions(), encoding, text); 
            m_SaveInProgress = false;

            // Proceed only if the file got actually saved
            if (fileMetaData != null) {
                // This is a little hackey that we do it here but it works
                m_TextEditorControl.NotifyThatTabGotClosed(m_TabToSave);

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

        private async Task<FileMetaData> PerformSave(EncryptionOptions options, TextEncoding encoding, string text) {
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

            SaveFileResult result = await FileHandler.SaveFileAsync(path, options, encoding, text);
            if (result.Status == SaveFileStatus.Success) {
                return result.FileMetaData;
            } else {
                DialogWindow.Show(
                    Application.Current.MainWindow,
                    $"Failed to save the file:\n{path}!\n{result.Exception.GetType()}\n{result.Exception.Message}",
                    "Saving Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return null;
            }
        }

        private void OnSecurityTypeSelectionChanged(EncryptionType type) {
            AESOptions.Visibility = type == EncryptionType.AES ? Visibility.Visible : Visibility.Hidden;
            RC4Options.Visibility = type == EncryptionType.RC4 ? Visibility.Visible : Visibility.Hidden;
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

        private EncryptionOptions BuildEncryptionOptions() {
            EncryptionType encryptionType = (EncryptionType)EncryptionTypeComboBox.SelectedItem;
            EncryptionOptions options;
            switch (encryptionType) {
                case EncryptionType.AES:
                    options = new EncryptionOptionsAES() {
                        KeySize = (int)AESKeySizeComboBox.SelectedItem,
                        Mode = (CipherMode)AESModeComboBox.SelectedItem,
                        Padding = (CipherPadding)AESPaddingComboBox.SelectedItem
                    };
                    break;
                case EncryptionType.RC4:
                    options = new EncryptionOptionsRC4() {
                        KeySize = (int)RC4KeySizeComboBox.SelectedItem,
                    };
                    break;
                default: throw new InvalidOperationException();
            }

            options.DigestType = (DigestType)DigestTypeComboBox.SelectedItem;
            return options;
        }

        private IEnumerable<T> GetEnumValuesWithout<T>(params T[] without) {
            return Enum.GetValues(typeof(T)).Cast<T>().Except(without);
        }

        private T GetDefaultEncryptionOptions<T>(EncryptionOptions options, EncryptionType type) where T : EncryptionOptions {
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