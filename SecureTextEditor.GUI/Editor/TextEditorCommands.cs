using System.Windows.Input;

namespace SecureTextEditor.GUI.Editor {
    /// <summary>
    /// Holds commands for the text editor.
    /// </summary>
    public static class TextEditorCommands {
        /// <summary>
        /// Command for zooming in.
        /// </summary>
        public static RoutedCommand ZoomInCommand { get; } = new RoutedCommand();
        /// <summary>
        /// Command for zooming out.
        /// </summary>
        public static RoutedCommand ZoomOutCommand { get; } = new RoutedCommand();
        /// <summary>
        /// Command for closing a tab.
        /// </summary>
        public static RoutedCommand CloseTabCommand { get; } = new RoutedCommand();

        static TextEditorCommands() {
            // Initialize commands with keyboard shortcuts
            ZoomInCommand.InputGestures.Add(new KeyGesture(Key.OemPlus, ModifierKeys.Control, "Crtl+Plus"));
            ZoomOutCommand.InputGestures.Add(new KeyGesture(Key.OemMinus, ModifierKeys.Control, "Crtl+Minus"));
            CloseTabCommand.InputGestures.Add(new KeyGesture(Key.W, ModifierKeys.Control));
        }
    }
}
