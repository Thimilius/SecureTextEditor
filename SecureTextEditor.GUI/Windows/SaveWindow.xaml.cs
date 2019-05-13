using Microsoft.Win32;
using SecureTextEditor.Core;
using SecureTextEditor.GUI.Editor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SecureTextEditor.GUI {
    /// <summary>
    /// Interaction logic for SaveWindow.xaml
    /// </summary>
    public partial class SaveWindow : Window {
        public SaveWindow() {
            InitializeComponent();

            // Set up UI
            CipherModeComboBox.ItemsSource = Enum.GetValues(typeof(CipherBlockMode)).Cast<CipherBlockMode>();
            CipherModeComboBox.SelectedItem = CipherBlockMode.CBC;
            CipherPaddingComboBox.ItemsSource = Enum.GetValues(typeof(CipherBlockPadding)).Cast<CipherBlockPadding>();
            CipherPaddingComboBox.SelectedItem = CipherBlockPadding.PKCS7;
        }

        private void CancelSave(object sender, RoutedEventArgs e) {
            Close();
        }

        private async void Save(object sender, RoutedEventArgs e) {
            TextEditorControl control = (Owner as MainWindow).TextEditorControl;
            TextEditorTab tab = control.CurrentTab;

            // Do the actual save
            WaitingIndicator.Visibility = Visibility.Visible;
            CipherBlockMode mode = (CipherBlockMode)CipherModeComboBox.SelectedItem;
            CipherBlockPadding padding = (CipherBlockPadding)CipherPaddingComboBox.SelectedItem;
            FileMetaData metaData = await FileHandler.SaveFileAsync(tab.Editor.Text, mode, padding, tab.FileMetaData.Encoding);

            // Update file meta data and header for the tab that got saved
            if (metaData != null) {
                // This is a little hackey that we do it here but it works
                control.ProcessClosingTabCounter(tab);

                // Set new meta data for alreay existing tab and update its header
                tab.FileMetaData = metaData;
                tab.SetHeader(metaData.FileName);
            }

            // Saved files are no longer dirty
            tab.Dirty = false;

            // We can close the dialog when finished
            Close();
        }
    }
}