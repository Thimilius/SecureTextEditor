using System.Windows;

namespace SecureTextEditor.GUI.Dialog {
    /// <summary>
    /// Interaction logic for a dialog window.
    /// </summary>
    public partial class DialogBox : Window {
        /// <summary>
        /// Creates a new dialog window.
        /// </summary>
        private DialogBox() {
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
            return Show(owner, message, caption, DialogBoxButton.OK, DialogBoxIcon.Information);
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
        public static bool Show(Window owner, string message, string caption, DialogBoxButton button, DialogBoxIcon icon) {
            // Create window and set properties
            var dialog = new DialogBox() {
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
        private static void SetButtons(DialogBox dialog, DialogBoxButton button) {
            // Set the right buttons
            switch (button) {
                case DialogBoxButton.YesNo:
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
        private static void SetIcon(DialogBox dialog, DialogBoxIcon icon) {
            // Set the right Font Awesome icon
            switch (icon) {
                case DialogBoxIcon.Information: dialog.IconText.Text = "\uf05a"; break;
                case DialogBoxIcon.Question: dialog.IconText.Text = "\uf059"; break;
                case DialogBoxIcon.Error: dialog.IconText.Text = "\uf06a"; break;
                case DialogBoxIcon.Key: dialog.IconText.Text = "\uf084"; break;
                default: dialog.IconText.Text = ""; break;
            }
        }
    }
}
