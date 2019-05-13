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
        private const int ZOOM_MIN_LIMIT = 4;
        private const int ZOOM_MAX_LIMIT = 60;

        private MainWindow m_Window;
        private TabControl m_TabControl;
        private int m_Zoom; // Actually describes font size

        private int m_NewTabCounter;
        private List<int> m_NewTabCounterList;

        public TextEditorTab CurrentTab { get; private set; }

        public event Action TabChanged;

        public TextEditorControl(MainWindow window, TabControl tabControl) {
            m_Window = window;
            m_TabControl = tabControl;
            m_NewTabCounter = 1;
            m_NewTabCounterList = new List<int>();

            m_TabControl.SelectionChanged += OnTabControlSelectionChanged;

            // Get zoom from config
            m_Zoom = AppConfig.Config.Zoom;
        }

        public void NewTab(string content) {
            // Figure out the counter for the new tab
            int counter;
            if (m_NewTabCounterList.Count > 0) {
                m_NewTabCounterList.Sort();
                counter = m_NewTabCounterList[0];
                m_NewTabCounterList.RemoveAt(0);
            } else {
                counter = m_NewTabCounter++;
            }

            string name = $"New {counter}";

            NewTab(content, new FileMetaData() {
                Encoding = AppConfig.Config.NewFileTextEncoding,
                FileName = name,
                FilePath = name,
                IsNew = true
            });
        } 

        public void NewTab(string content, FileMetaData fileMetaData) {
            // Currently we have a maximum number of concurrently open tabs
            if (m_TabControl.Items.Count == MAX_TABS) {
                return;
            }

            // FIXME: We should use an object pool here to avoid using unnecessary memory
            var tab = new TextEditorTab(this, fileMetaData, content);
            var item = tab.TabItem;
            var editor = tab.Editor;

            // Set zoom
            editor.FontSize = m_Zoom;

            // Subscribe to tab drag and drop events
            item.PreviewMouseMove += OnTabItemPreviewMouseMove;
            item.Drop += OnTabItemDrop;
            item.GiveFeedback += OnTabItemGiveFeedback;

            // Subscribe to text events
            editor.TextChanged += (s, e) => m_Window.UpdateUI();
            editor.SelectionChanged += (s, e) => m_Window.UpdateUI();

            // Add the tab to current tabs and focus it
            m_TabControl.Items.Add(item);
            FocusTab(tab);
        }

        public void CloseTab(TextEditorTab tab) {
            // We don't bother closing the tab if its the last one and empty
            if (m_TabControl.Items.Count == 1 && tab.Editor.Text == "") {
                return;
            }

            // Prompt the user for saving if the file is dirty
            if (tab.Dirty) {
                m_Window.PromptSaveDialog();
            }

            var item = tab.TabItem;
            // Remove the tab
            m_TabControl.Items.Remove(item);

            ProcessClosingTabCounter(tab);

            // If we have no tabs open any more, open a new empty one
            if (!m_TabControl.HasItems) {
                NewTab("");
            }
        }

        public void ZoomIn() {
            m_Zoom += 2;
            SetZoom();
        }

        public void ZoomOut() {
            m_Zoom -= 2;
            SetZoom();
        }

        public void ZoomReset() {
            m_Zoom = 16;
            SetZoom();
        }

        public void ProcessClosingTabCounter(TextEditorTab tab) {
            // Only process "new" tabs that were not saved before
            if (tab.FileMetaData.IsNew) {
                // This is a little hardcoded but thats fine
                m_NewTabCounterList.Add(int.Parse(tab.FileMetaData.FileName.Substring(4)));
            }
        }

        private void SetZoom() {
            ClampZoom();
            CurrentTab.Editor.FontSize = m_Zoom;
            AppConfig.Config.Zoom = m_Zoom;
        }

        private void ClampZoom() {
            if (m_Zoom < ZOOM_MIN_LIMIT) {
                m_Zoom = ZOOM_MIN_LIMIT;
            } else if (m_Zoom > ZOOM_MAX_LIMIT) {
                m_Zoom = ZOOM_MAX_LIMIT;
            }
        }

        private void FocusTab(TextEditorTab tab) {
            tab.Focus();
            CurrentTab = tab;
        }

        private void OnTabControlSelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (e.Source is TabControl) {
                if (e.AddedItems.Count > 0) {
                    if (e.AddedItems[0] is TabItem tab) {
                        // Update the current tab
                        CurrentTab = tab.Tag as TextEditorTab;

                        SetZoom();

                        // Invoke event
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
