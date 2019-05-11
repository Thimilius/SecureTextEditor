using System.Windows;

namespace SecureTextEditor.GUI {
    /// <summary>
    /// Interaction logic for DialogWindow.xaml
    /// </summary>
    public partial class DialogWindow : Window {
        public DialogWindow() {
            InitializeComponent();
        }

        private void OnCancel(object sender, RoutedEventArgs e) {
            // Set result and close window
            DialogResult = false;
            Close();
        }

        private void OnOk(object sender, RoutedEventArgs e) {
            // Set result and close window
            DialogResult = true;
            Close();
        }

        public static bool? Show(Window owner, string message, string caption) {
            return Show(owner, message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static bool? Show(Window owner, string message, string caption, MessageBoxButton button, MessageBoxImage icon) {
            // Create window and set properties
            var dialog = new DialogWindow() {
                Owner = owner,
                Title = caption
            };
            dialog.MessageText.Text = message;

            SetButtons(dialog, button);
            SetIcon(dialog, icon);

            // Show window and return result
            return dialog.ShowDialog();
        }

        private static void SetButtons(DialogWindow dialog, MessageBoxButton button) {
            // Set the right buttons
            switch (button) {
                case MessageBoxButton.YesNoCancel:
                case MessageBoxButton.YesNo:
                    dialog.CancelButton.Content = "No";
                    dialog.OkButton.Content = "Yes";
                    break;
                default:
                    dialog.CancelButton.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private static void SetIcon(DialogWindow dialog, MessageBoxImage icon) {
            // Set the right Font Awesome icon
            switch (icon) {
                case MessageBoxImage.Question: dialog.IconText.Text = "\uf059"; break;
                case MessageBoxImage.Information: dialog.IconText.Text = "\uf05a"; break;
                case MessageBoxImage.Warning: dialog.IconText.Text = "\uf071"; break;
                case MessageBoxImage.Error: dialog.IconText.Text = "\uf06a"; break;
                default: dialog.IconText.Text = ""; break;
            }
        }
    }
}
