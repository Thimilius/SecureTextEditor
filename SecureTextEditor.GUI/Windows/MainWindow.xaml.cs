﻿using AdonisUI;
using Microsoft.Win32;
using SecureTextEditor.Core;
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

namespace SecureTextEditor.GUI {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private enum Theme {
            LightMode,
            DarkMode
        }

        public enum Encoding {
            UTF8,
            ASCII
        }

        private Theme m_Theme;
        private Encoding m_Encoding;
        public System.Text.Encoding CurrentEncoding => m_Encoding == Encoding.UTF8 ? System.Text.Encoding.UTF8 : System.Text.Encoding.ASCII;

        public string CurrentText => Editor.Text;

        public MainWindow() { 
            InitializeComponent();

            m_Encoding = Encoding.UTF8;
        }

        private void ThemeChanged(object sender, RoutedEventArgs e) {
            MenuItem menuItem = sender as MenuItem;
            ChangeTheme(menuItem.Header.Equals("Light Mode") ? Theme.LightMode : Theme.DarkMode, menuItem);
        }

        private void EncodingChanged(object sender, RoutedEventArgs e) {
            MenuItem menuItem = sender as MenuItem;
            ChangeEncoding(menuItem.Header.Equals("UTF-8") ? Encoding.UTF8 : Encoding.ASCII, menuItem);
        }

        private void EditorTextChanged(object sender, RoutedEventArgs e) {
            UpdateEditorStatus(sender);
        }

        private void EditorSelectionChanged(object sender, RoutedEventArgs e) {
            UpdateEditorStatus(sender);
        }

        private void UpdateEditorStatus(object sender) {
            TextBox editor = sender as TextBox;
            LinesLabel.Text = $"Lines: {editor.LineCount}";
            int caretIndex = editor.CaretIndex;
            int lineIndex = editor.GetLineIndexFromCharacterIndex(caretIndex);
            LineLabel.Text = $"Ln: {lineIndex + 1}";
            ColumnLabel.Text = $"Col: {caretIndex - editor.GetCharacterIndexFromLineIndex(lineIndex)}";
            SelectionLabel.Text = $"Sel: {editor.SelectionLength}";
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

        private void ChangeTheme(Theme theme, MenuItem clickedItem) {
            m_Theme = theme;
            Uri locator = theme == Theme.DarkMode ? ResourceLocator.DarkColorScheme : ResourceLocator.LightColorScheme;
            ResourceLocator.SetColorScheme(Application.Current.Resources, locator);

            // Update UI
            foreach (MenuItem item in ThemeMenu.Items.OfType<MenuItem>()) {
                item.IsChecked = false;
            }
            clickedItem.IsChecked = true;
        }

        private void ChangeEncoding(Encoding encoding, MenuItem clickedItem) {
            m_Encoding = encoding;

            // Update UI
            foreach (MenuItem item in EncodingMenu.Items.OfType<MenuItem>()) {
                item.IsChecked = false;
            }
            clickedItem.IsChecked = true;
            EncodingText.Text = $"Encoding: {clickedItem.Header}"; 
        }

        private void CloseApp(object sender, RoutedEventArgs e) {
            // TODO: Check for unsaved changes
            Application.Current.Shutdown();
        }

        private void Open(object sender, RoutedEventArgs e) {
            string text = FileHandler.OpenFile();
            if (text != null) {
                Editor.Text = text;
            }
        }

        private void Save(object sender, RoutedEventArgs e) {
            ShowSaveWindow();
        }

        private void ShowSaveWindow() {
            Window window = new SaveWindow {
                Owner = this,
            };
            window.ShowDialog();
        }
    }
}
