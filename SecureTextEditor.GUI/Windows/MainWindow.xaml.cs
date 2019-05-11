using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using AdonisUI;
using Microsoft.Win32;
using SecureTextEditor.Core;
using SecureTextEditor.GUI.Config;
using SecureTextEditor.GUI.Editor;

namespace SecureTextEditor.GUI {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public Encoding CurrentEncoding => Encoding.UTF8;

        public TextEditorControl TextEditorControl { get; private set; }

        public MainWindow(string path) { 
            InitializeComponent();

            // We need to set the inital theme based on config
            ChangeTheme(AppConfig.Config.Theme);

            // Create text editor
            TextEditorControl = new TextEditorControl(this, EditorTabControl);
            TextEditorControl.TabChanged += OnTabChanged;

            // If we get passed in a path try to load in the file
            if (path != null) {
                // TODO: Better abstract this logic
                var file = FileHandler.OpenFile(path, Path.GetFileName(path));
                if (file != null) {
                    TextEditorControl.NewTab(file.Text, file.Encoding, file.FileName);
                    UpdateEncodingUI();
                }
            } else {
                TextEditorControl.NewTab("");
            }

            // Subscribe to global events
            EditorTabControl.MouseWheel += OnZoomChanged;
            ThemeCheckBoxLightMode.Click += (s, e) => ChangeTheme(Theme.LightMode);
            ThemeCheckBoxDarkMode.Click += (s, e) => ChangeTheme(Theme.DarkMode);
            EncodingCheckBoxASCII.Click += (s, e) => ChangeEncoding(TextEncoding.ASCII);
            EncodingCheckBoxUTF8.Click += (s, e) => ChangeEncoding(TextEncoding.UTF8);
        }

        public void UpdateEditorStatus() {
            TextBox editor = TextEditorControl.CurrentTab.Editor;

            // Update status bar texts and take into account empty text editor
            LinesLabel.Text = $"Lines: {Math.Max(1, editor.LineCount)}";
            int caretIndex = editor.CaretIndex;
            int lineIndex = Math.Max(0, editor.GetLineIndexFromCharacterIndex(caretIndex));
            LineLabel.Text = $"Ln: {lineIndex + 1}";
            int charIndex = Math.Max(0, editor.GetCharacterIndexFromLineIndex(lineIndex));
            ColumnLabel.Text = $"Col: {(caretIndex - charIndex) + 1}";
            SelectionLabel.Text = $"Sel: {editor.SelectionLength}";
        }

        private void OnExit(object sender, RoutedEventArgs e) {
            if (TextEditorControl.CurrentTab.Dirty) {
                // TODO: Prompt unsaved warning
            }
            Application.Current.Shutdown();
        }

        private void OnNew(object sender, RoutedEventArgs e) {
            TextEditorControl.NewTab("");
        }

        private void OnOpen(object sender, RoutedEventArgs e) {
            var file = FileHandler.OpenFile();
            if (file != null) {
                TextEditorControl.NewTab(file.Text, file.Encoding, file.FileName);
                UpdateEncodingUI();
            }
        }

        private void OnSave(object sender, RoutedEventArgs e) {
            ShowSaveWindow();
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
            // Handles zoom change via mouse wheel
        }

        private void OnCloseTab(object sender, RoutedEventArgs e) {
            TextEditorControl.CloseTab(TextEditorControl.CurrentTab);
        }

        private void OnEncodingChanged(object sender, RoutedEventArgs e) {
            // Update encoding checkboxes
            EncodingCheckBoxASCII.IsChecked = TextEditorControl.CurrentTab.TextEncoding == TextEncoding.ASCII;
            EncodingCheckBoxUTF8.IsChecked = TextEditorControl.CurrentTab.TextEncoding == TextEncoding.UTF8;
        }

        private void OnTabChanged() {
            UpdateEncodingUI();
            UpdateEditorStatus();
        }

        private void ShowSaveWindow() {
            Window window = new SaveWindow {
                Owner = this,
            };
            window.ShowDialog();
        }

        private void ChangeEncoding(TextEncoding encoding) {
            TextEditorControl.CurrentTab.TextEncoding = encoding;

            UpdateEncodingUI();
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

        private void UpdateEncodingUI() {
            string TextEncodingToString(TextEncoding textEncoding) {
                switch (textEncoding) {
                    case TextEncoding.ASCII: return "ASCII";
                    case TextEncoding.UTF8: return "UTF-8";
                    default: return "Unknown";
                }
            }

            // Update encoding checkboxes
            var encoding = TextEditorControl.CurrentTab.TextEncoding;
            EncodingCheckBoxASCII.IsChecked = encoding == TextEncoding.ASCII;
            EncodingCheckBoxUTF8.IsChecked = encoding == TextEncoding.UTF8;

            EncodingLabel.Text = $"Encoding: {TextEncodingToString(encoding)}";
        }
    }
}
