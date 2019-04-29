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

        private enum Encoding {
            UTF8,
            ASCII
        }

        private Theme m_Theme;
        private Encoding m_Encoding;

        public MainWindow() { 
            InitializeComponent();

            m_Encoding = Encoding.UTF8;
        }

        private void ThemeChanged(object sender, RoutedEventArgs e) {
            MenuItem menuItem = sender as MenuItem;
            ChangeTheme(menuItem.Header.Equals("Light Mode") ? Theme.LightMode : Theme.DarkMode, menuItem);
        }

        private void EncodingChanged(object sender, RoutedEventArgs e) {
            MenuItem menuItem = sender as MenuItem;
            ChangeEncoding(menuItem.Header.Equals("UTF-8") ? Encoding.UTF8 : Encoding.ASCII, menuItem);
        }

        private void ChangeTheme(Theme theme, MenuItem clickedItem) {
            m_Theme = theme;
            Uri locator = theme == Theme.DarkMode ? ResourceLocator.DarkColorScheme : ResourceLocator.LightColorScheme;
            ResourceLocator.SetColorScheme(Application.Current.Resources, locator);

            // Update UI
            foreach (MenuItem item in ThemeMenu.Items.OfType<MenuItem>()) {
                item.IsChecked = false;
            }
            clickedItem.IsChecked = true;
        }

        private void ChangeEncoding(Encoding encoding, MenuItem clickedItem) {
            m_Encoding = encoding;

            // Update UI
            foreach (MenuItem item in EncodingMenu.Items.OfType<MenuItem>()) {
                item.IsChecked = false;
            }
            clickedItem.IsChecked = true;
            EncodingText.Text = $"Encoding: {clickedItem.Header}"; 
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
