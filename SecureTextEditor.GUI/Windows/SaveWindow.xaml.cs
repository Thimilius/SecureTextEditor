using SecureTextEditor.Core;
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
        }

        private void CancelSave(object sender, RoutedEventArgs e) {
            Close();
        }

        private async void Save(object sender, RoutedEventArgs e) {
            WaitingIndicator.Visibility = Visibility.Visible;

            MainWindow window = Owner as MainWindow;
            await SaveAsnyc(window.CurrentEncoding, window.CurrentText);

            CancelButton.IsEnabled = false;
            SaveButton.IsEnabled = false;

            Close();
        }
        
        private async Task SaveAsnyc(Encoding encoding, string text) {
            await Task.Run(() => {
                CryptoPlaceholder crypto = new CryptoPlaceholder(encoding);
                string cipher = crypto.Encrypt(text);
                File.WriteAllText("save.stxt", cipher);
            });
            await Task.Delay(100);
        }
    }
}