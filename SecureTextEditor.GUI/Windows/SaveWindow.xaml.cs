using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using SecureTextEditor.Core;
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

            // FIXME: CTS block mode can only be used when message is more than one block in size
            //        and should therefore not be an option if that is note the case

            // FIXME: No padding should be handled when text is not block size aligned

            // Set up UI
            SecurityTypeComboBox.ItemsSource = Enum.GetValues(typeof(SecurityType)).Cast<SecurityType>();
            CipherBlockModeComboBox.ItemsSource = Enum.GetValues(typeof(CipherBlockMode)).Cast<CipherBlockMode>();
            CipherBlockPaddingComboBox.ItemsSource = Enum.GetValues(typeof(CipherBlockPadding)).Cast<CipherBlockPadding>();

            // Set default options from config
            SecurityTypeComboBox.SelectedItem = tab.FileMetaData.EncryptionOptions.Type;
            CipherBlockModeComboBox.SelectedItem = tab.FileMetaData.EncryptionOptions.BlockMode;
            CipherBlockPaddingComboBox.SelectedItem = tab.FileMetaData.EncryptionOptions.BlockPadding;
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

            // Gather options for saving
            SecurityType type = (SecurityType)SecurityTypeComboBox.SelectedItem;
            CipherBlockMode mode = (CipherBlockMode)CipherBlockModeComboBox.SelectedItem;
            CipherBlockPadding padding = (CipherBlockPadding)CipherBlockPaddingComboBox.SelectedItem;
            EncryptionOptions options = new EncryptionOptions() { Type = type, BlockMode = mode, BlockPadding = padding };
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

        protected override void OnClosing(CancelEventArgs e) {
            // We want to cancel the window closing when a save is in progress
            if (m_SaveInProgress) {
                e.Cancel = true;
            }
            base.OnClosing(e);
        }
    }
}