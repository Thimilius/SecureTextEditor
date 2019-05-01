using Microsoft.Win32;
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
            MainWindow window = Owner as MainWindow;
            
            WaitingIndicator.Visibility = Visibility.Visible;
            await FileHandler.SaveFileAsync(window.CurrentText, window.CurrentEncoding);

            CancelButton.IsEnabled = false;
            SaveButton.IsEnabled = false;

            Close();
        }
    }
}