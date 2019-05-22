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
        private TextEditorControl m_TextEditorControl;
        private TextEditorTab m_TabToSave;
        private bool m_SaveInProgress;

        public SaveWindow(TextEditorControl control, TextEditorTab tab) {
            InitializeComponent();

            m_TextEditorControl = control;
            m_TabToSave = tab;

            // FIXME: CTS block mode should only be usable when message is more than one block in size
            //        and should therefore not be an option if that is note the case

            // FIXME: Padding and Mode should be two way dependent

            // Set up UI
            EncryptionTypeComboBox.ItemsSource = GetEnumValues<EncryptionType>();
            AESKeySizeComboBox.ItemsSource = new int[] { 128, 192, 256 };
            AESModeComboBox.ItemsSource = GetEnumValues(CipherMode.None);
            AESDigestTypeComboBox.ItemsSource = GetEnumValues<DigestType>();
            RC4DigestTypeComboBox.ItemsSource = GetEnumValues<DigestType>();
            AESPaddingComboBox.ItemsSource = GetEnumValues<CipherPadding>();
            RC4KeySizeComboBox.ItemsSource = new int[] { 128, 192, 256 };

            // Set default options
            EncryptionOptions options = tab.FileMetaData.EncryptionOptions;
            EncryptionTypeComboBox.SelectedItem = options.Type;
            AESKeySizeComboBox.SelectedItem = options.KeySize;
            RC4KeySizeComboBox.SelectedItem = options.KeySize;
            AESDigestTypeComboBox.SelectedItem = options.DigestType;
            RC4DigestTypeComboBox.SelectedItem = options.DigestType;

            EncryptionOptionsAES optionsAES = GetEncryptionOptions<EncryptionOptionsAES>(options, EncryptionType.AES);
            AESModeComboBox.SelectedItem = optionsAES.Mode;
            AESPaddingComboBox.SelectedItem = optionsAES.Padding;

            // Set up events
            EncryptionTypeComboBox.SelectionChanged += (s, e) => OnSecurityTypeSelectionChanged((EncryptionType)EncryptionTypeComboBox.SelectedItem);
            AESPaddingComboBox.SelectionChanged += (s, e) => OnAESPaddingSelectionChanged((CipherPadding)AESPaddingComboBox.SelectedItem);

            // Set up initial ui visibility
            OnSecurityTypeSelectionChanged(options.Type);
            OnAESPaddingSelectionChanged(optionsAES.Padding);
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

            // Figure out correct cipher type depending on selected security type
            EncryptionType encryptionType = (EncryptionType)EncryptionTypeComboBox.SelectedItem;

            // Gather options for saving
            EncryptionOptions options = null;
            switch (encryptionType) {
                case EncryptionType.AES:
                    options = new EncryptionOptionsAES() {
                        DigestType = (DigestType)AESDigestTypeComboBox.SelectedItem,
                        Mode = (CipherMode)AESModeComboBox.SelectedItem,
                        Padding = (CipherPadding)AESPaddingComboBox.SelectedItem
                    };
                    break;
                case EncryptionType.RC4:
                    options = new EncryptionOptionsRC4() {
                        DigestType = (DigestType)RC4DigestTypeComboBox.SelectedItem
                    };
                    break;
                default:
                    break;
            }
            options.KeySize = (int)AESKeySizeComboBox.SelectedItem;
            TextEncoding encoding = m_TabToSave.FileMetaData.Encoding;
            string text = m_TabToSave.Editor.Text;

            // Do the actual save 
            m_SaveInProgress = true;
            FileMetaData metaData = await FileHandler.SaveFileAsync(options, encoding, text);
            m_SaveInProgress = false;

            // Proceed only if the file got actually saved
            if (metaData != null) {
                // This is a little hackey that we do it here but it works
                m_TextEditorControl.ProcessClosingTabCounter(m_TabToSave);

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
                AESModeComboBox.ItemsSource = GetEnumValues(CipherMode.ECB, CipherMode.CBC, CipherMode.None);
                AESModeComboBox.SelectedIndex = 0;
            } else {
                AESModeComboBox.ItemsSource = GetEnumValues(CipherMode.CTS, CipherMode.CTR, CipherMode.CFB, CipherMode.OFB, CipherMode.None);
                AESModeComboBox.SelectedIndex = 0;
            }
        }

        private IEnumerable<T> GetEnumValues<T>(params T[] without) {
            return Enum.GetValues(typeof(T)).Cast<T>().Except(without);
        }

        private T GetEncryptionOptions<T>(EncryptionOptions options, EncryptionType type) where T : EncryptionOptions {
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