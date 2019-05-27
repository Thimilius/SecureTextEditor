using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using SecureTextEditor.Core;
using SecureTextEditor.Core.Cipher;
using SecureTextEditor.Core.Digest;
using SecureTextEditor.Core.Options;
using SecureTextEditor.GUI.Config;
using SecureTextEditor.GUI.Editor;

namespace SecureTextEditor.GUI {
    /// <summary>
    /// Interaction logic for SaveWindow.xaml
    /// </summary>
    public partial class SaveWindow : Window {
        private ITextEditorControl m_TextEditorControl;
        private ITextEditorTab m_TabToSave;
        private bool m_SaveInProgress;
        private bool m_CTSPaddingAvailable;

        public SaveWindow(ITextEditorControl control, ITextEditorTab tab) {
            InitializeComponent();

            m_TextEditorControl = control;
            m_TabToSave = tab;
            m_CTSPaddingAvailable = m_TabToSave.Editor.Text.Length >= CipherEngine.BLOCK_SIZE;
            
            // Set up UI
            EncryptionTypeComboBox.ItemsSource = GetEnumValues<EncryptionType>();
            AESKeySizeComboBox.ItemsSource = CipherEngine.AES_ACCEPTED_KEYS;
            AESDigestTypeComboBox.ItemsSource = GetEnumValues<DigestType>();
            AESPaddingComboBox.ItemsSource = GetEnumValues<CipherPadding>();
            RC4DigestTypeComboBox.ItemsSource = GetEnumValues<DigestType>();
            RC4KeySizeComboBox.ItemsSource = CipherEngine.RC4_ACCEPTED_KEYS;

            // Set default options
            EncryptionOptions options = tab.FileMetaData.EncryptionOptions;
            EncryptionTypeComboBox.SelectedItem = options.Type;
            AESKeySizeComboBox.SelectedItem = options.KeySize;
            RC4KeySizeComboBox.SelectedItem = options.KeySize;
            AESDigestTypeComboBox.SelectedItem = options.DigestType;
            RC4DigestTypeComboBox.SelectedItem = options.DigestType;

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
            TextEncoding encoding = m_TabToSave.FileMetaData.Encoding;
            string text = m_TabToSave.Editor.Text;
            FileMetaData metaData = await FileHandler.SaveFileAsync(BuildEncryptionOptions(), encoding, text);
            m_SaveInProgress = false;

            // Proceed only if the file got actually saved
            if (metaData != null) {
                // This is a little hackey that we do it here but it works
                m_TextEditorControl.NotifyThatTabGotClosed(m_TabToSave);

                // Set new meta data for alreay existing tab and update its header
                m_TabToSave.FileMetaData = metaData;
                m_TabToSave.SetHeader(metaData.FileName);

                // We can close the dialog when finished
                Close();
            }

            // Turn off the save indicator and turn on buttons again
            SavingIndicator.Visibility = Visibility.Hidden;
            CancelButton.IsEnabled = true;
            SaveButton.IsEnabled = true;
        }

        private void OnSecurityTypeSelectionChanged(EncryptionType type) {
            AESOptions.Visibility = type == EncryptionType.AES ? Visibility.Visible : Visibility.Hidden;
            RC4Options.Visibility = type == EncryptionType.RC4 ? Visibility.Visible : Visibility.Hidden;
        }

        private void OnAESPaddingSelectionChanged(CipherPadding padding) {
            if (padding == CipherPadding.None) {
                // Only modes that have no padding should be available
                if (m_CTSPaddingAvailable) {
                    AESModeComboBox.ItemsSource = GetEnumValues(CipherMode.ECB, CipherMode.CBC, CipherMode.None);
                } else {
                    AESModeComboBox.ItemsSource = GetEnumValues(CipherMode.CTS, CipherMode.ECB, CipherMode.CBC, CipherMode.None);
                }
            } else {
                // Only modes that have a padding should be available
                AESModeComboBox.ItemsSource = GetEnumValues(CipherMode.CTS, CipherMode.CTR, CipherMode.CFB, CipherMode.OFB, CipherMode.None);
            }

            // When changing just select the first element
            AESModeComboBox.SelectedIndex = 0;
        }

        private EncryptionOptions BuildEncryptionOptions() {
            EncryptionType encryptionType = (EncryptionType)EncryptionTypeComboBox.SelectedItem;
            switch (encryptionType) {
                case EncryptionType.AES:
                    return new EncryptionOptionsAES() {
                        DigestType = (DigestType)AESDigestTypeComboBox.SelectedItem,
                        KeySize = (int)AESKeySizeComboBox.SelectedItem,
                        Mode = (CipherMode)AESModeComboBox.SelectedItem,
                        Padding = (CipherPadding)AESPaddingComboBox.SelectedItem
                    };
                case EncryptionType.RC4:
                    return new EncryptionOptionsRC4() {
                        DigestType = (DigestType)RC4DigestTypeComboBox.SelectedItem,
                        KeySize = (int)RC4KeySizeComboBox.SelectedItem,
                    };
                default: throw new InvalidOperationException();
            }
        }

        private IEnumerable<T> GetEnumValues<T>(params T[] without) {
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