using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AdonisUI;
using SecureTextEditor.Crypto;
using SecureTextEditor.GUI.Config;
using SecureTextEditor.GUI.Editor;

namespace SecureTextEditor.GUI {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public ITextEditorControl TextEditorControl { get; private set; }

        public MainWindow() { 
            InitializeComponent();

            // We need to set the inital theme based on config
            ChangeTheme(AppConfig.Config.Theme);

            // Create text editor
            TextEditorControl = new TextEditorControl(this, EditorTabControl);
            TextEditorControl.TabChanged += UpdateUI;

            // Open an inital empty tab
            TextEditorControl.NewTab("");

            // Subscribe to global events
            EditorTabControl.MouseWheel += OnZoomChanged;
            Closing += OnWindowClosing;
            ThemeCheckBoxLightMode.Click += (s, e) => ChangeTheme(Theme.LightMode);
            ThemeCheckBoxDarkMode.Click += (s, e) => ChangeTheme(Theme.DarkMode);
            EncodingCheckBoxASCII.Click += (s, e) => ChangeEncoding(TextEncoding.ASCII);
            EncodingCheckBoxUTF8.Click += (s, e) => ChangeEncoding(TextEncoding.UTF8);
        }

        public void OpenFile(string path) {
            // Open actual file
            (string text, FileMetaData metaData) = FileHandler.OpenFile(TextEditorControl, path);

            if (text != null && metaData != null) {
                // Open new tab for the file
                TextEditorControl.NewTab(text, metaData);

                // Update UI
                UpdateEncodingStatus();
            }
        }

        public void UpdateUI() {
            UpdateEncodingStatus();
            UpdateEditorStatus();
            UpdateWindowTitle();
        }

        public void PromptSaveDialog(ITextEditorTab tab) {
            // Show question dialog
            bool save = DialogWindow.Show(this,
                $"Do you want to save \"{tab.FileMetaData.FileName}\" before closing?",
                "Save",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (save) {
                PromptSaveWindow(tab);
            }
        }

        private void PromptSaveDialogs() {
            // Loop through every dirty tab
            foreach (var tab in TextEditorControl.Tabs.Where(t => t.FileMetaData.IsDirty)) {
                // Focus the tab and prompt save dialog for it
                TextEditorControl.FocusTab(tab);
                PromptSaveDialog(tab);
            }
        }

        private void PromptSaveWindow(ITextEditorTab tab) {
            // Open and show the save dialog
            Window window = new SaveWindow(TextEditorControl, tab) {
                Owner = this,
            };
            window.ShowDialog();

            UpdateUI();
        }

        private void OnExit(object sender, EventArgs e) {
            Close();
        }

        private void OnNew(object sender, RoutedEventArgs e) {
            TextEditorControl.NewTab("");
        }

        private void OnOpen(object sender, RoutedEventArgs e) {
            OpenFile(null);
        }

        private void OnSave(object sender, RoutedEventArgs e) {
            PromptSaveWindow(TextEditorControl.CurrentTab);
        }

        private void OnWindowClosing(object sender, CancelEventArgs e) {
            PromptSaveDialogs();
        }

        private void OnZoomIn(object sender, RoutedEventArgs e) {
            TextEditorControl.ZoomIn();
        }

        private void OnZoomOut(object sender, RoutedEventArgs e) {
            TextEditorControl.ZoomOut();
        }

        private void OnZoomReset(object sender, RoutedEventArgs e) {
            TextEditorControl.ZoomReset();
        }

        private void OnZoomChanged(object sender, MouseWheelEventArgs e) {
            // Handles zoom change via mouse wheel when control is held down
            if (Keyboard.PrimaryDevice.Modifiers == ModifierKeys.Control) {
                if (e.Delta > 0) {
                    TextEditorControl.ZoomIn();
                } else {
                    TextEditorControl.ZoomOut();
                }
            }
        }

        private void OnCloseTab(object sender, RoutedEventArgs e) {
            TextEditorControl.CloseTab(TextEditorControl.CurrentTab);
        }

        private void OnEncodingChanged(object sender, RoutedEventArgs e) {
            // Update encoding checkboxes
            EncodingCheckBoxASCII.IsChecked = TextEditorControl.CurrentTab.FileMetaData.Encoding == TextEncoding.ASCII;
            EncodingCheckBoxUTF8.IsChecked = TextEditorControl.CurrentTab.FileMetaData.Encoding == TextEncoding.UTF8;
        }

        private void ChangeEncoding(TextEncoding encoding) {
            TextEditorControl.CurrentTab.FileMetaData.Encoding = encoding;

            UpdateEncodingStatus();
        }

        private void ChangeTheme(Theme theme) {
            // Store theme in config
            AppConfig.Config.Theme = theme;

            // Make actual visual change
            Uri locator = theme == Theme.LightMode ? ResourceLocator.LightColorScheme : ResourceLocator.DarkColorScheme;
            ResourceLocator.SetColorScheme(Application.Current.Resources, locator);

            // Update theme checkboxes
            ThemeCheckBoxLightMode.IsChecked = theme == Theme.LightMode;
            ThemeCheckBoxDarkMode.IsChecked = theme == Theme.DarkMode;
        }

        private void UpdateEncodingStatus() {
            string TextEncodingToString(TextEncoding textEncoding) {
                switch (textEncoding) {
                    case TextEncoding.ASCII: return "ASCII";
                    case TextEncoding.UTF8: return "UTF-8";
                    default: return "Unknown";
                }
            }

            // Update encoding checkboxes
            var encoding = TextEditorControl.CurrentTab.FileMetaData.Encoding;
            EncodingCheckBoxASCII.IsChecked = encoding == TextEncoding.ASCII;
            EncodingCheckBoxUTF8.IsChecked = encoding == TextEncoding.UTF8;

            EncodingLabel.Text = $"Encoding: {TextEncodingToString(encoding)}";
        }

        private void UpdateEditorStatus() {
            // Update status bar texts and take into account empty text editor
            TextBox editor = TextEditorControl.CurrentTab.Editor;
            LinesLabel.Text = $"Lines: {Math.Max(1, editor.LineCount)}";
            int caretIndex = editor.CaretIndex;
            int lineIndex = Math.Max(0, editor.GetLineIndexFromCharacterIndex(caretIndex));
            LineLabel.Text = $"Ln: {lineIndex + 1}";
            int charIndex = Math.Max(0, editor.GetCharacterIndexFromLineIndex(lineIndex));
            ColumnLabel.Text = $"Col: {(caretIndex - charIndex) + 1}";
            SelectionLabel.Text = $"Sel: {editor.SelectionLength}";
        }

        private void UpdateWindowTitle() {
            Title = $"Secure Text Editor - {TextEditorControl.CurrentTab.FileMetaData.FilePath}";
        }
    }
}
