using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SecureTextEditor.GUI.Editor {
    public class TextEditorTab {
        private TextEditorControl m_Control;
        private TextBlock m_Header;

        public TabItem TabItem { get; private set; }
        public TextBox Editor { get; private set; }

        public FileMetaData FileMetaData { get; set; }

        public TextEditorTab(TextEditorControl control, FileMetaData fileMetaData, string content) {
            FileMetaData = fileMetaData;
            m_Control = control;

            Editor = new TextBox {
                Text = content
            };
            FileMetaData.IsDirty = false;

            // Create UI
            CreateUI(fileMetaData.FileName);
        }

        public void Focus() {
            TabItem.Focus();
            Editor.Focus();
        }

        public void SetHeader(string header) {
            m_Header.Text = header;
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
            m_Header = new TextBlock() {
                Text = header
            };
            stackPanel.Children.Add(m_Header);

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

            Editor.TextChanged += (s, e) => {
                if (!FileMetaData.IsDirty) {
                    FileMetaData.IsDirty = true;
                    // Show that the file is dirty
                    m_Header.Text += "*";
                }
            };
        }

        private void OnClose(object sender, RoutedEventArgs e) {
            m_Control.CloseTab(this);
        }
    }
}
