using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SecureTextEditor.Core;
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

            // FIXME: CTS block mode can only be used when message is more than one block in size
            //        and should therefore not be an option if that is note the case

            // Set up UI
            SecurityTypeComboBox.ItemsSource = Enum.GetValues(typeof(SecurityType)).Cast<SecurityType>();
            AESKeySizeComboBox.ItemsSource = new int[] { 128, 192, 256 };
            AESModeComboBox.ItemsSource = Enum.GetValues(typeof(CipherMode)).Cast<CipherMode>();
            // NOTE: Should we allow no padding?
            AESPaddingComboBox.ItemsSource = Enum.GetValues(typeof(CipherPadding)).Cast<CipherPadding>().Where(p => p != CipherPadding.None);
            ARC4KeySizeComboBox.ItemsSource = new int[] { 128, 192, 256 };

            // Set default options from config
            EncryptionOptions options = tab.FileMetaData.EncryptionOptions;
            SecurityTypeComboBox.SelectedItem = options.Type;
            AESKeySizeComboBox.SelectedItem = options.KeySize;
            AESModeComboBox.SelectedItem = options.AESMode;
            AESPaddingComboBox.SelectedItem = options.AESPadding;
            ARC4KeySizeComboBox.SelectedItem = options.KeySize;

            // Set up events
            SecurityTypeComboBox.SelectionChanged += (s, e) => OnSecurityTypeSelectionChanged((SecurityType)SecurityTypeComboBox.SelectedItem);
            AESModeComboBox.SelectionChanged += (s, e) => OnAESModeSelectionChanged((CipherMode)AESModeComboBox.SelectedItem);

            // Set up initial ui visibility
            OnSecurityTypeSelectionChanged(options.Type);
            OnAESModeSelectionChanged(options.AESMode);
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
            SecurityType securityType = (SecurityType)SecurityTypeComboBox.SelectedItem;
            CipherType cipherType = CipherType.Block;
            if (securityType == SecurityType.ARC4) {
                cipherType = CipherType.Stream;
            }
            
            // Gather options for saving
            EncryptionOptions options = new EncryptionOptions() {
                Type = securityType,
                KeySize = (int)AESKeySizeComboBox.SelectedItem,
                CipherType = cipherType,
                AESMode = (CipherMode)AESModeComboBox.SelectedItem,
                AESPadding = (CipherPadding)AESPaddingComboBox.SelectedItem
            };
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

        private void OnSecurityTypeSelectionChanged(SecurityType type) {
            AESOptions.Visibility = type == SecurityType.AES ? Visibility.Visible : Visibility.Hidden;
            ARC4Options.Visibility = type == SecurityType.ARC4 ? Visibility.Visible : Visibility.Hidden;
        }

        private void OnAESModeSelectionChanged(CipherMode mode) {
            AESPaddingComboBox.IsEnabled = mode == CipherMode.ECB || mode == CipherMode.CBC;
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