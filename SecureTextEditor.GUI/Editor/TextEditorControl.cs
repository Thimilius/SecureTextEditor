using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SecureTextEditor.File;
using SecureTextEditor.GUI.Config;

namespace SecureTextEditor.GUI.Editor {
    public class TextEditorControl : ITextEditorControl {
        private const int MAX_TABS = 10;
        private const int ZOOM_MIN_LIMIT = 4;
        private const int ZOOM_MAX_LIMIT = 60;

        private MainWindow m_Window;
        private TabControl m_TabControl;
        private int m_Zoom; // Actually describes font size

        private int m_NewTabCounter;
        private List<int> m_NewTabCounterList;

        public ITextEditorTab CurrentTab { get; private set; }

        public IEnumerable<ITextEditorTab> Tabs => m_TabControl.Items.Cast<TabItem>().Select(i => (ITextEditorTab)i.Tag);

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
            string name = $"New {GetTabCounter()}";

            NewTab(content, new TextEditorTabMetaData() {
                FileMetaData = new FileMetaData() {
                    Encoding = AppConfig.Config.NewFileTextEncoding,
                    EncryptionOptions = AppConfig.Config.DefaultEncryptionOptions[AppConfig.Config.DefaultEncryptionType],
                    FileName = name,
                    FilePath = name
                }, 
                IsNew = true,
                IsDirty = false
            });
        } 

        public void NewTab(string content, TextEditorTabMetaData metaData) {
            // Currently we have a maximum number of concurrently open tabs
            if (m_TabControl.Items.Count == MAX_TABS) {
                return;
            }

            // We can reuse an already open new and empty tab
            // but only if we have actual content to display
            if (IsLastTabNewAndEmpty() && content != "") {
                // We "simulate" closing the new tab
                ITextEditorTab lastTab = Tabs.First();

                NotifyThatTabGotClosed(lastTab);

                lastTab.MetaData = metaData;
                lastTab.Editor.Text = content;

                // We have to manually reset header and dirtyness
                // because of already subscribed callbacks
                lastTab.SetHeader(metaData.FileMetaData.FileName);
                lastTab.MetaData.IsDirty = false;

                FocusTab(lastTab);

                return;
            }

            // FIXME: We should use an object pool here to avoid using unnecessary memory
            var tab = new TextEditorTab(this, metaData, content);
            var item = tab.TabItem;
            var editor = tab.Editor;

            // We want to set the tab as the tag object for later use
            item.Tag = tab;

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

        public void CloseTab(ITextEditorTab tab) {
            // We don't bother closing the tab if its the last one
            if (IsLastTabNewAndEmpty()) {
                return;
            }

            // Prompt the user for saving if the file is dirty
            if (tab.MetaData.IsDirty) {
                m_Window.PromptSaveDialog(tab);
            }

            var item = tab.TabItem;
            // Remove the tab
            m_TabControl.Items.Remove(item);

            NotifyThatTabGotClosed(tab);

            // If we have no tabs open any more, open a new empty one
            if (!m_TabControl.HasItems) {
                NewTab("");
            }
        }

        public void FocusTab(ITextEditorTab tab) {
            CurrentTab = tab;
            tab.FocusControls();
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

        public void NotifyThatTabGotClosed(ITextEditorTab tab) {
            // Only process "new" tabs that were not saved before
            if (tab.MetaData.IsNew) {
                // This is a little hardcoded but thats fine
                m_NewTabCounterList.Add(int.Parse(tab.MetaData.FileMetaData.FileName.Substring(4)));
            }
        }

        private void SetZoom() {
            ClampZoom();
            CurrentTab.Editor.FontSize = m_Zoom;
            AppConfig.Config.Zoom = m_Zoom;
        }

        private void ClampZoom() {
            // We clamp the zoom value to its limits
            if (m_Zoom < ZOOM_MIN_LIMIT) {
                m_Zoom = ZOOM_MIN_LIMIT;
            } else if (m_Zoom > ZOOM_MAX_LIMIT) {
                m_Zoom = ZOOM_MAX_LIMIT;
            }
        }

        private int GetTabCounter() {
            // Figure out the counter for the new tab
            if (m_NewTabCounterList.Count > 0) {
                m_NewTabCounterList.Sort();
                int counter = m_NewTabCounterList[0];
                m_NewTabCounterList.RemoveAt(0);
                return counter;
            } else {
                return m_NewTabCounter++;
            }
        }

        private bool IsLastTabNewAndEmpty() {
            if (m_TabControl.Items.Count == 1) {
                ITextEditorTab tab = Tabs.First();
                return tab.MetaData.IsNew && tab.Editor.Text == "";
            } else {
                return false;
            }
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
            // Check we clicked on an actual TabItem
            if (!(e.Source is TabItem tabItem)) {
                return;
            }

            // Notify system of drag and drop 
            if (Mouse.PrimaryDevice.LeftButton == MouseButtonState.Pressed) {
                DragDrop.DoDragDrop(tabItem, tabItem, DragDropEffects.All);
            }
        }

        private void OnTabItemDrop(object sender, DragEventArgs e) {
            TabItem FindTargetTabItem(object originalSource) {
                var current = originalSource as DependencyObject;

                // Go the visual tree up to find our actual TabItem
                while (current != null) {
                    // Check for found TabItem
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
