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
            CipherTypeComboBox.ItemsSource = Enum.GetValues(typeof(CipherType)).Cast<CipherType>();
            KeySizeComboBox.ItemsSource = new int[] { 128, 192, 256 };
            CipherBlockModeComboBox.ItemsSource = Enum.GetValues(typeof(CipherBlockMode)).Cast<CipherBlockMode>();
            // NOTE: Should we allow no padding?
            CipherBlockPaddingComboBox.ItemsSource = Enum.GetValues(typeof(CipherBlockPadding)).Cast<CipherBlockPadding>().Where(p => p != CipherBlockPadding.None);

            // Set default options from config
            EncryptionOptions options = tab.FileMetaData.EncryptionOptions;
            SecurityTypeComboBox.SelectedItem = options.Type;
            CipherTypeComboBox.SelectedItem = options.CipherType;
            KeySizeComboBox.SelectedItem = options.CipherKeySize;
            CipherBlockModeComboBox.SelectedItem = options.CipherBlockMode;
            CipherBlockPaddingComboBox.SelectedItem = options.CipherBlockPadding;

            // Add events for visibility control
            CipherTypeComboBox.SelectionChanged += (o, e) => OnCipherTypeSelectionChanged((CipherType)e.AddedItems[0]);

            // Do initial visibilty control
            OnCipherTypeSelectionChanged((CipherType)CipherTypeComboBox.SelectedItem);
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
            EncryptionOptions options = new EncryptionOptions() {
                Type = (SecurityType)SecurityTypeComboBox.SelectedItem,
                CipherType = (CipherType)CipherTypeComboBox.SelectedItem,
                CipherKeySize = (int)KeySizeComboBox.SelectedItem,
                CipherBlockMode = (CipherBlockMode)CipherBlockModeComboBox.SelectedItem,
                CipherBlockPadding = (CipherBlockPadding)CipherBlockPaddingComboBox.SelectedItem
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

        private void OnCipherTypeSelectionChanged(CipherType type) {
            CipherBlockOptions.Visibility = type == CipherType.Block ? Visibility.Visible : Visibility.Hidden;
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