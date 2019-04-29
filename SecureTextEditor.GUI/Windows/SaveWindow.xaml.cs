using System;
using System.Collections.Generic;
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
            // TODO: Do this over a binding
            CancelButton.IsEnabled = false;
            SaveButton.IsEnabled = false;
            await Task.Delay(2000);
            Close();
        }
    }
}