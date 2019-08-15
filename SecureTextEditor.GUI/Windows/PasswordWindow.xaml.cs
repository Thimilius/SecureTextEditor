using System.Security;
using System.Windows;
using System.Windows.Input;

namespace SecureTextEditor.GUI {
    /// <summary>
    /// Interaction logic for the password window.
    /// </summary>
    public partial class PasswordWindow : Window {
        /// <summary>
        /// The password typed in.
        /// </summary>
        public SecureString Password { get; private set; }

        /// <summary>
        /// Creates a new password window.
        /// </summary>
        public PasswordWindow(Window owner, string message) {
            InitializeComponent();

            Owner = owner;
            MessageText.Text = message;
            PasswordTextBox.PasswordChanged += (s, e) => OnPasswordChanged(PasswordTextBox.SecurePassword);
            OnPasswordChanged(PasswordTextBox.SecurePassword);
            PasswordTextBox.Focus();
        }

        /// <summary>
        /// Callback when the password changes.
        /// </summary>
        /// <param name="password">The password entered in</param>
        private void OnPasswordChanged(SecureString password) {
            SubmitButton.IsEnabled = password != null && password.Length != 0;
        }

        /// <summary>
        /// Callback when the cancel button gets clicked.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event parameters</param>
        private void OnCancel(object sender, RoutedEventArgs e) {
            // Set result and close window
            DialogResult = false;
            Password = null;
            Close();
        }

        /// <summary>
        /// Callback when the submit button gets clicked.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event parameters</param>
        private void OnSubmit(object sender, RoutedEventArgs e) {
            // Set result and close window
            DialogResult = true;
            Password = PasswordTextBox.SecurePassword;
            Close();
        }

        /// <summary>
        /// Callback whenever the window recieves a key down event.
        /// </summary>
        /// <param name="e">The event parameters</param>
        protected override void OnKeyDown(KeyEventArgs e) {
            base.OnKeyDown(e);

            // Submit on enter
            if (e.Key == Key.Enter) {
                OnSubmit(null, null);
            }
        }

        /// <summary>
        /// Clears out the password.
        /// </summary>
        public void Clear() {
            PasswordTextBox.Clear();
        }
    }
}
