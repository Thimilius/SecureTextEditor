using System;
using System.Linq;
using System.Windows;
using SecureTextEditor.Core;
using SecureTextEditor.GUI.Editor;

namespace SecureTextEditor.GUI {
    /// <summary>
    /// Interaction logic for SaveWindow.xaml
    /// </summary>
    public partial class SaveWindow : Window {
        private TextEditorControl m_TextEditorControl;
        private TextEditorTab m_TabToSave;

        public SaveWindow(TextEditorControl control, TextEditorTab tab) {
            InitializeComponent();

            m_TextEditorControl = control;
            m_TabToSave = tab;

            // TODO: Remember save options for files
            // TODO: Set default options from config
            // Set up UI
            CipherBlockModeComboBox.ItemsSource = Enum.GetValues(typeof(CipherBlockMode)).Cast<CipherBlockMode>();
            CipherBlockModeComboBox.SelectedItem = CipherBlockMode.CBC;
            CipherBlockPaddingComboBox.ItemsSource = Enum.GetValues(typeof(CipherBlockPadding)).Cast<CipherBlockPadding>();
            CipherBlockPaddingComboBox.SelectedItem = CipherBlockPadding.PKCS7;
        }

        private void CancelSave(object sender, RoutedEventArgs e) {
            // When canceling we can just close the window
            Close();
        }

        private async void Save(object sender, RoutedEventArgs e) {
            // Turn on the save indicator
            SavingIndicator.Visibility = Visibility.Visible;

            // Do the actual save
            CipherBlockMode mode = (CipherBlockMode)CipherBlockModeComboBox.SelectedItem;
            CipherBlockPadding padding = (CipherBlockPadding)CipherBlockPaddingComboBox.SelectedItem;
            string text = m_TabToSave.Editor.Text;
            TextEncoding encoding = m_TabToSave.FileMetaData.Encoding;
            FileMetaData metaData = await FileHandler.SaveFileAsync(text, mode, padding, encoding);

            // Update file meta data and header for the tab that got saved
            if (metaData != null) {
                // This is a little hackey that we do it here but it works
                m_TextEditorControl.ProcessClosingTabCounter(m_TabToSave);

                // Set new meta data for alreay existing tab and update its header
                m_TabToSave.FileMetaData = metaData;
                m_TabToSave.SetHeader(metaData.FileName);
            }

            // Saved files are no longer dirty
            m_TabToSave.Dirty = false;

            // We can close the dialog when finished
            Close();
        }
    }
}