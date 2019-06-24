using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SecureTextEditor.GUI.Editor {
    public class TextEditorTab : ITextEditorTab {
        private TextEditorControl m_Control;
        private TextBlock m_Header;

        public TabItem TabItem { get; private set; }
        public TextBox Editor { get; private set; }

        public TextEditorTabMetaData MetaData { get; set; }

        public TextEditorTab(TextEditorControl control, TextEditorTabMetaData metaData, string content) {
            MetaData = metaData;
            m_Control = control;

            Editor = new TextBox {
                Text = content
            };
            MetaData.IsDirty = false;

            // Create UI
            CreateUI(metaData.FileMetaData.FileName);
        }

        public void Focus() {
            TabItem.Focus();
            Editor.Focus();
        }

        public void SetHeader(string header) {
            m_Header.Text = header;
        }

        private void CreateUI(string header) {
            ContextMenu contextMenu = new ContextMenu();
            string shortcutDisplay = (TextEditorCommands.CloseTabCommand.InputGestures[0] as KeyGesture).DisplayString;
            MenuItem closeMenuItem = new MenuItem() {
                Header = "Close",
                InputGestureText = shortcutDisplay
            };
            contextMenu.Items.Add(closeMenuItem);

            StackPanel stackPanel = new StackPanel() {
                ContextMenu = contextMenu
            };
            m_Header = new TextBlock() {
                Text = header
            };
            stackPanel.Children.Add(m_Header);

            Button closeButton = new Button();
            stackPanel.Children.Add(closeButton);

            TabItem = new TabItem() {
                Header = stackPanel,
                Content = Editor,
                Tag = this
            };

            // Subscribe to events
            closeButton.Click += OnClose;
            closeMenuItem.Click += OnClose;
            Editor.TextChanged += OnTextChanged;
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e) {
            if (!MetaData.IsDirty) {
                // Set the file dirty and show it in header
                MetaData.IsDirty = true;
                m_Header.Text += "*";
            }
        }

        private void OnClose(object sender, RoutedEventArgs e) {
            m_Control.CloseTab(this);
        }
    }
}
