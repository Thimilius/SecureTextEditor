using AdonisUI;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SecureTextEditor.GUI {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private enum Theme {
            LightMode,
            DarkMode
        }

        public MainWindow() { 
            InitializeComponent();
        }

        private void ThemeChanged(object sender, RoutedEventArgs e) {
            MenuItem menuItem = sender as MenuItem;
            ChangeTheme(menuItem.IsChecked ? Theme.DarkMode : Theme.LightMode);
        }

        private void ChangeTheme(Theme theme) {
            Uri locator = theme == Theme.DarkMode ? ResourceLocator.DarkColorScheme : ResourceLocator.LightColorScheme;
            ResourceLocator.SetColorScheme(Application.Current.Resources, locator);
        }

        private void CloseApp(object sender, RoutedEventArgs e) {
            // TODO: Check for unsaved changes
            Application.Current.Shutdown();
        }

        private void Save(object sender, RoutedEventArgs e) {
            ShowSaveWindow();
        }

        private void ShowSaveWindow() {
            Window window = new SaveWindow {
                Owner = this,
            };
            window.ShowDialog();
        }
    }
}
