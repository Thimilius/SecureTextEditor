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
            CipherModeComboBox.ItemsSource = Enum.GetValues(typeof(CipherMode)).Cast<CipherMode>();
            CipherModeComboBox.SelectedItem = CipherMode.CBC;
            CipherPaddingComboBox.ItemsSource = Enum.GetValues(typeof(CipherPadding)).Cast<CipherPadding>();
            CipherPaddingComboBox.SelectedItem = CipherPadding.PKCS7;
        }

        private void CancelSave(object sender, RoutedEventArgs e) {
            Close();
        }

        private async void Save(object sender, RoutedEventArgs e) {
            TextEditorTab tab = (Owner as MainWindow).TextEditorControl.CurrentTab;

            // Do the actual save
            WaitingIndicator.Visibility = Visibility.Visible;
            CipherMode mode = (CipherMode)CipherModeComboBox.SelectedItem;
            CipherPadding padding = (CipherPadding)CipherPaddingComboBox.SelectedItem;
            FileMetaData metaData = await FileHandler.SaveFileAsync(tab.Editor.Text, mode, padding, tab.FileMetaData.Encoding);

            // Update file meta data and header for the tab that got saved
            if (metaData != null) {
                tab.FileMetaData = metaData;
                tab.SetHeader(metaData.FileName);
            }

            CancelButton.IsEnabled = false;
            SaveButton.IsEnabled = false;

            Close();
        }
    }
}