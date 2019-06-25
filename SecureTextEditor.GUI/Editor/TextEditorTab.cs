using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SecureTextEditor.GUI.Editor {
    /// <summary>
    /// Implements a text editor tab.
    /// </summary>
    public class TextEditorTab : ITextEditorTab {
        /// <summary>
        /// Saves a reference to the control this tab belongs to.
        /// </summary>
        private readonly ITextEditorControl m_Control;
        /// <summary>
        /// Holds the header that belongs to the tab.
        /// </summary>
        private TextBlock m_Header;

        /// <summary>
        /// <see cref="ITextEditorTab.TabItem"/>
        /// </summary>
        public TabItem TabItem { get; private set; }
        /// <summary>
        /// <see cref="ITextEditorTab.Editor"/>
        /// </summary>
        public TextBox Editor { get; private set; }
        /// <summary>
        /// <see cref="ITextEditorTab.MetaData"/>
        /// </summary>
        public TextEditorTabMetaData MetaData { get; set; }

        /// <summary>
        /// Creates a new text editor tab with given parameters.
        /// </summary>
        /// <param name="control">The control the tab belongs to</param>
        /// <param name="metaData">The meta data associated with the tab</param>
        /// <param name="content">The text content that should be displayed</param>
        public TextEditorTab(ITextEditorControl control, TextEditorTabMetaData metaData, string content) {
            MetaData = metaData;
            m_Control = control;

            Editor = new TextBox {
                Text = content
            };
            MetaData.IsDirty = false;

            // Create UI
            CreateUI(metaData.FileMetaData.FileName);
        }

        /// <summary>
        /// <see cref="ITextEditorTab.Focus"/>
        /// </summary>
        public void Focus() {
            TabItem.Focus();
            Editor.Focus();
        }

        /// <summary>
        /// <see cref="ITextEditorTab.SetHeader(string)"/>
        /// </summary>
        public void SetHeader(string header) {
            m_Header.Text = header;
        }

        /// <summary>
        /// Creates the ui for the tab.
        /// </summary>
        /// <param name="header">The header that should initially be displayed</param>
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

        /// <summary>
        /// Callback that gets called whenever the text changes.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event parameters</param>
        private void OnTextChanged(object sender, TextChangedEventArgs e) {
            if (!MetaData.IsDirty) {
                // Set the file dirty and show it in header
                MetaData.IsDirty = true;
                m_Header.Text += "*";
            }
        }

        /// <summary>
        /// Callback that gets called when the user clicks the close button of the tab.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event parameters</param>
        private void OnClose(object sender, RoutedEventArgs e) {
            m_Control.CloseTab(this);
        }
    }
}
