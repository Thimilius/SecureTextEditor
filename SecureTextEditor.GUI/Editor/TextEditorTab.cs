using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SecureTextEditor.Core;

namespace SecureTextEditor.GUI.Editor {
    public class TextEditorTab {
        private TextEditorControl m_Control;

        public TabItem TabItem { get; private set; }
        public TextBox Editor { get; private set; }

        public TextEncoding TextEncoding { get; set; }
        public bool Dirty { get; set; }

        public TextEditorTab(TextEditorControl control, string header, string content, TextEncoding textEncoding) {
            TextEncoding = textEncoding;
            Dirty = false;
            m_Control = control;
            Editor = new TextBox {
                Text = content
            };

            // Create UI
            CreateUI(header);
        }

        public void Focus() {
            TabItem.Focus();
            Editor.Focus();
        }

        private void CreateUI(string header) {
            var contextMenu = new ContextMenu();
            var closeMenuItem = new MenuItem() {
                Header = "Close",
                InputGestureText = (TextEditorCommands.CloseTabCommand.InputGestures[0] as KeyGesture).GetDisplayStringForCulture(null)
            };
            contextMenu.Items.Add(closeMenuItem);

            var stackPanel = new StackPanel() {
                ContextMenu = contextMenu
            };
            stackPanel.Children.Add(new TextBlock() {
                Text = header
            });

            var closeButton = new Button();
            stackPanel.Children.Add(closeButton);

            TabItem = new TabItem() {
                Header = stackPanel,
                Content = Editor,
                Tag = this
            };

            // Subscribe to events
            closeButton.Click += OnClose;
            closeMenuItem.Click += OnClose;

            Editor.TextInput += (s, e) => Dirty = true;
        }

        private void OnClose(object sender, RoutedEventArgs e) {
            m_Control.CloseTab(this);
        }
    }
}
