using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AdonisUI;
using Microsoft.Win32;
using SecureTextEditor.Core;
using SecureTextEditor.GUI.Config;

namespace SecureTextEditor.GUI {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public Encoding CurrentEncoding => Encoding.UTF8;

        public string CurrentText => Editor.Text;

        public MainWindow() { 
            InitializeComponent();

            // We need to set the inital theme based on config
            ChangeTheme(AppConfig.Config.Theme);

            // Initialize global events
            ThemeCheckBoxLightMode.Click += (s, e) => ChangeTheme(Theme.LightMode);
            ThemeCheckBoxDarkMode.Click += (s, e) => ChangeTheme(Theme.DarkMode);

            Editor.TextChanged += (s, e) => UpdateEditorStatus(s as TextBox);
            Editor.SelectionChanged += (s, e) => UpdateEditorStatus(s as TextBox);

            // We want to directly set the focus on the editor
            Editor.Focus();
            UpdateEditorStatus(Editor);
        }

        private void TabControlPreviewMouseMove(object sender, MouseEventArgs e) {
            if (!(e.Source is TabItem tabItem)) {
                return;
            }

            if (Mouse.PrimaryDevice.LeftButton == MouseButtonState.Pressed) {
                DragDrop.DoDragDrop(tabItem, tabItem, DragDropEffects.All);
            }
        }

        private void TabControlDrop(object sender, DragEventArgs e) {
            var tabItemTarget = e.Source as TabItem;
            var tabItemSource = e.Data.GetData(typeof(TabItem)) as TabItem;

            if (!tabItemTarget.Equals(tabItemSource)) {
                var tabControl = tabItemTarget.Parent as TabControl;
                int targetIndex = tabControl.Items.IndexOf(tabItemTarget);

                tabControl.Items.Remove(tabItemSource);
                tabControl.Items.Insert(targetIndex, tabItemSource);

                tabItemSource.Focus();
            }
        }

        private void TabControlGiveFeedback(object sender, GiveFeedbackEventArgs e) {
            e.UseDefaultCursors = false;
            e.Handled = true;
        }

        private void OnExit(object sender, RoutedEventArgs e) {
            // TODO: Check for unsaved changes
            Application.Current.Shutdown();
        }

        private void OnOpen(object sender, RoutedEventArgs e) {
            string text = FileHandler.OpenFile();
            if (text != null) {
                Editor.Text = text;
            }
        }

        private void OnSave(object sender, RoutedEventArgs e) {
            ShowSaveWindow();
        }

        private void OnZoomIn(object sender, RoutedEventArgs e) {
            Editor.FontSize++;
        }

        private void OnZoomOut(object sender, RoutedEventArgs e) {
            Editor.FontSize--;
        }

        private void OnZoomReset(object sender, RoutedEventArgs e) {
            Editor.FontSize = 16;
        }

        private void OnCloseTab(object sender, RoutedEventArgs e) {
            MessageBox.Show("Hello there!");
        }

        private void ShowSaveWindow() {
            Window window = new SaveWindow {
                Owner = this,
            };
            window.ShowDialog();
        }

        private void UpdateEditorStatus(TextBox editor) {
            // Update status bar texts and take into account empty text editor
            LinesLabel.Text = $"Lines: {Math.Max(1, editor.LineCount)}";
            int caretIndex = editor.CaretIndex;
            int lineIndex = Math.Max(0, editor.GetLineIndexFromCharacterIndex(caretIndex));
            LineLabel.Text = $"Ln: {lineIndex + 1}";
            int charIndex = Math.Max(0, editor.GetCharacterIndexFromLineIndex(lineIndex));
            ColumnLabel.Text = $"Col: {(caretIndex - charIndex) + 1}";
            SelectionLabel.Text = $"Sel: {editor.SelectionLength}";
        }

        private void ChangeTheme(Theme theme) {
            // Store theme in config
            AppConfig.Config.Theme = theme;

            // Make actual visual change
            Uri locator = theme == Theme.LightMode ? ResourceLocator.LightColorScheme : ResourceLocator.DarkColorScheme;
            ResourceLocator.SetColorScheme(Application.Current.Resources, locator);

            // Update UI
            ThemeCheckBoxLightMode.IsChecked = theme == Theme.LightMode;
            ThemeCheckBoxDarkMode.IsChecked = theme == Theme.DarkMode;
        }
    }
}
