using System.Windows;

namespace SecureTextEditor.GUI {
    /// <summary>
    /// Interaction logic for a dialog window.
    /// </summary>
    public partial class DialogWindow : Window {
        /// <summary>
        /// Creates a new dialog window.
        /// </summary>
        public DialogWindow() {
            InitializeComponent();
        }

        /// <summary>
        /// Opens and shows a dialog window with given parameters.
        /// The default displays an OK Button with an information message.
        /// </summary>
        /// <param name="owner">The owner window of the dialog box</param>
        /// <param name="message">The message to display</param>
        /// <param name="caption">The title of the dialog window</param>
        /// <returns>True if ok was clicked, false if cancel was clicked</returns>
        public static bool Show(Window owner, string message, string caption) {
            return Show(owner, message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Opens and shows a dialog window with given parameters.
        /// </summary>
        /// <param name="owner">The owner window of the dialog box</param>
        /// <param name="message">The message to display</param>
        /// <param name="caption">The title of the dialog window</param>
        /// <param name="button">The buttons to display</param>
        /// <param name="icon">The icon to display</param>
        /// <returns>True if ok was clicked, false if cancel was clicked</returns>
        public static bool Show(Window owner, string message, string caption, MessageBoxButton button, MessageBoxImage icon) {
            // Create window and set properties
            var dialog = new DialogWindow() {
                Owner = owner,
                Title = caption
            };

            // Setup message, buttons and icon
            dialog.MessageText.Text = message;
            SetButtons(dialog, button);
            SetIcon(dialog, icon);

            // Show window and return result
            return dialog.ShowDialog().Value;
        }

        /// <summary>
        /// Callback when the cancel button gets clicked.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event parameters</param>
        private void OnCancel(object sender, RoutedEventArgs e) {
            // Set result and close window
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Callback when the ok button gets clicked.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event parameters</param>
        private void OnOk(object sender, RoutedEventArgs e) {
            // Set result and close window
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Sets up the buttons for a dialog window.
        /// </summary>
        /// <param name="dialog">The dialog window to set up</param>
        /// <param name="button">The buttons to set up</param>
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

        /// <summary>
        /// Sets up the icon or a dialog window.
        /// </summary>
        /// <param name="dialog">The dialog window to set up</param>
        /// <param name="icon">The icon to set up</param>
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
