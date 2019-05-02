using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SecureTextEditor.Core;
using SecureTextEditor.GUI.Config;

namespace SecureTextEditor.GUI.Editor {
    public class TextEditorControl {
        private const int MAX_TABS = 10;

        private MainWindow m_Window;
        private TabControl m_TabControl;
        private int m_NewTabCounter;

        public TextEditorTab CurrentTab { get; private set; }

        public event Action TabChanged;

        public TextEditorControl(MainWindow window, TabControl tabControl) {
            m_Window = window;
            m_TabControl = tabControl;
            m_NewTabCounter = 1;

            m_TabControl.SelectionChanged += OnTabControlSelectionChanged;
        }

        public void NewTab(string content) => NewTab(content, AppConfig.Config.NewFileTextEncoding, $"New {m_NewTabCounter++}");

        public void NewTab(string content, TextEncoding textEncoding, string header) {
            if (m_TabControl.Items.Count == MAX_TABS) {
                // TODO: Inform user
                return;
            }

            // FIXME: We should use an object pool here to avoid using unnecessary memory
            var tab = new TextEditorTab(this, header, content, textEncoding);
            var item = tab.TabItem;
            var editor = tab.Editor;

            // Subscribe to tab drag and drop events
            item.PreviewMouseMove += OnTabItemPreviewMouseMove;
            item.Drop += OnTabItemDrop;
            item.GiveFeedback += OnTabItemGiveFeedback;

            // Subscribe to text events
            editor.TextChanged += (s, e) => m_Window.UpdateEditorStatus();
            editor.SelectionChanged += (s, e) => m_Window.UpdateEditorStatus();

            // Add the tab to current tabs and focus it
            m_TabControl.Items.Add(item);
            FocusTab(tab);
        }

        public void CloseTab(TextEditorTab tab) {
            if (m_TabControl.Items.Count == 1 && tab.Editor.Text == "") {
                return;
            }

            if (tab.Dirty) {
                // TODO: Prompt unsaved warning
            }

            var item = tab.TabItem;
            // Remove the tab
            m_TabControl.Items.Remove(item);

            // If we have no tabs open any more, open a new empty one
            if (!m_TabControl.HasItems) {
                NewTab("");
            }
        }

        private void FocusTab(TextEditorTab tab) {
            tab.Focus();
            CurrentTab = tab;
        }

        // TODO: Support zoom via mouse wheel

        private void OnTabControlSelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (e.Source is TabControl) {
                if (e.AddedItems.Count > 0) {
                    if (e.AddedItems[0] is TabItem tab) {
                        CurrentTab = tab.Tag as TextEditorTab;
                        CurrentTab.UpdateStatus();
                        TabChanged?.Invoke();
                    }
                }
            }
        }

        private void OnTabItemPreviewMouseMove(object sender, MouseEventArgs e) {
            if (!(e.Source is TabItem tabItem)) {
                return;
            }

            if (Mouse.PrimaryDevice.LeftButton == MouseButtonState.Pressed) {
                DragDrop.DoDragDrop(tabItem, tabItem, DragDropEffects.All);
            }
        }

        private void OnTabItemDrop(object sender, DragEventArgs e) {
            TabItem FindTargetTabItem(object originalSource) {
                var current = originalSource as DependencyObject;

                // Go the visual tree up to find our actual TabItem
                while (current != null) {
                    if (current is TabItem tabItem) {
                        return tabItem;
                    }

                    current = VisualTreeHelper.GetParent(current);
                }

                return null;
            }

            var tabItemTarget = FindTargetTabItem(e.OriginalSource);
            var tabItemSource = e.Data.GetData(typeof(TabItem)) as TabItem;

            if (!tabItemTarget.Equals(tabItemSource)) {
                var tabControl = tabItemTarget.Parent as TabControl;
                int targetIndex = tabControl.Items.IndexOf(tabItemTarget);

                tabControl.Items.Remove(tabItemSource);
                tabControl.Items.Insert(targetIndex, tabItemSource);

                tabItemSource.Focus();
            }
        }

        private void OnTabItemGiveFeedback(object sender, GiveFeedbackEventArgs e) {
            // We do not want to display the ugly cursors while doing drag and drop
            e.UseDefaultCursors = false;
            e.Handled = true;
        }
    }
}
