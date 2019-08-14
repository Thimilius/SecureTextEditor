using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SecureTextEditor.File.Handler;
using SecureTextEditor.GUI.Config;

namespace SecureTextEditor.GUI.Editor {
    /// <summary>
    /// Implements a text editor control.
    /// </summary>
    public class TextEditorControl : ITextEditorControl {
        /// <summary>
        /// Defines the maximum amount of tabs that can be open at the same time.
        /// </summary>
        private const int MAX_TABS = 10;

        /// <summary>
        /// Defines the minimum zoom level.
        /// </summary>
        private const int ZOOM_MIN_LIMIT = 4;
        /// <summary>
        /// Defines the maximum zoom level.
        /// </summary>
        private const int ZOOM_MAX_LIMIT = 60;
        /// <summary>
        /// Defines the default zoom level.
        /// </summary>
        private const int ZOOM_DEFAULT = 16;
        /// <summary>
        /// Defines the zoom change on zoom in and zoom out.
        /// </summary>
        private const int ZOOM_CHANGE = 2;

        /// <summary>
        /// Holds a reference to the main window this control belongs to.
        /// </summary>
        private readonly EditorWindow m_Window;
        /// <summary>
        /// Holds a reference to the tab control that belongs to this control.
        /// </summary>
        private readonly TabControl m_TabControl;

        private readonly List<int> m_NewTabCounterList;
        /// <summary>
        /// Holds the current new tab counter.
        /// </summary>
        private int m_NewTabCounter;

        /// <summary>
        /// Holds the current zoom (Simply measured in font size).
        /// </summary>
        private int m_Zoom;

        /// <summary>
        /// <see cref="ITextEditorControl.CurrentTab"/>
        /// </summary>
        public ITextEditorTab CurrentTab { get; private set; }
        /// <summary>
        /// <see cref="ITextEditorControl.Tabs"/>
        /// </summary>
        public IEnumerable<ITextEditorTab> Tabs => m_TabControl.Items.Cast<TabItem>().Select(i => (ITextEditorTab)i.Tag);

        /// <summary>
        /// <see cref="ITextEditorControl.TabChanged"/>
        /// </summary>
        public event Action TabChanged;

        /// <summary>
        /// Creates a new text editor control with given parameters.
        /// </summary>
        /// <param name="window">The window the text editor belongs to</param>
        /// <param name="tabControl">The tab control that should belong to the text editor</param>
        public TextEditorControl(EditorWindow window, TabControl tabControl) {
            m_Window = window;
            m_TabControl = tabControl;
            m_NewTabCounter = 1;
            m_NewTabCounterList = new List<int>();

            m_TabControl.SelectionChanged += OnTabControlSelectionChanged;

            // Get zoom from config
            m_Zoom = AppConfig.Config.Zoom;
        }

        /// <summary>
        /// <see cref="ITextEditorControl.NewTab(string)"/>
        /// </summary>
        public void NewTab(string content) {
            string name = $"New {GetNewTabCounter()}";

            NewTab(content, new TextEditorTabMetaData() {
                FileMetaData = new FileMetaData() {
                    Encoding = AppConfig.Config.NewFileTextEncoding,
                    EncryptionOptions = AppConfig.Config.DefaultEncryptionOptions[AppConfig.Config.DefaultCipherType],
                    FileName = name,
                    FilePath = name
                }, 
                IsNew = true,
                IsDirty = false
            });
        }

        /// <summary>
        /// <see cref="ITextEditorControl.NewTab(string, TextEditorTabMetaData)"/>
        /// </summary>
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

                SelectTab(lastTab);

                return;
            }

            // Create a new text editor tab
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
            SelectTab(tab);
        }

        /// <summary>
        /// <see cref="ITextEditorControl.CloseTab(ITextEditorTab)"/>
        /// </summary>
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

        /// <summary>
        /// <see cref="ITextEditorControl.SelectTab(ITextEditorTab)"/>
        /// </summary>
        public void SelectTab(ITextEditorTab tab) {
            CurrentTab = tab;
            tab.Focus();
        }

        /// <summary>
        /// <see cref="ITextEditorControl.ZoomIn"/>
        /// </summary>
        public void ZoomIn() {
            SetZoom(m_Zoom + ZOOM_CHANGE);
        }

        /// <summary>
        /// <see cref="ITextEditorControl.ZoomOut"/>
        /// </summary>
        public void ZoomOut() {
            SetZoom(m_Zoom - ZOOM_CHANGE);
        }

        /// <summary>
        /// <see cref="ITextEditorControl.ZoomReset"/>
        /// </summary>
        public void ZoomReset() {
            SetZoom(ZOOM_DEFAULT);
        }

        /// <summary>
        /// <see cref="ITextEditorControl.NotifyThatTabGotSaved(ITextEditorTab)"/>
        /// </summary>
        public void NotifyThatTabGotSaved(ITextEditorTab tab) {
            NotifyThatTabGotClosed(tab);
        }

        /// <summary>
        /// Notify that a tab got closed.
        /// </summary>
        /// <param name="tab">The tab that got closed</param>
        private void NotifyThatTabGotClosed(ITextEditorTab tab) {
            // Only process "new" tabs that were not saved before
            if (tab.MetaData.IsNew) {
                // This is a little hardcoded but thats fine
                m_NewTabCounterList.Add(int.Parse(tab.MetaData.FileMetaData.FileName.Substring(4)));
            }
        }

        /// <summary>
        /// Sets the new zoom.
        /// </summary>
        private void SetZoom(int zoom) {
            m_Zoom = zoom;

            ClampZoom();
            CurrentTab.Editor.FontSize = zoom;

            // Save zoom in config
            AppConfig.Config.Zoom = zoom;
        }

        /// <summary>
        /// Clamps the zoom to its minimum and maximum limit.
        /// </summary>
        private void ClampZoom() {
            // We clamp the zoom value to its limits
            if (m_Zoom < ZOOM_MIN_LIMIT) {
                m_Zoom = ZOOM_MIN_LIMIT;
            } else if (m_Zoom > ZOOM_MAX_LIMIT) {
                m_Zoom = ZOOM_MAX_LIMIT;
            }
        }

        /// <summary>
        /// Gets the current tab counter for a new tab.
        /// </summary>
        /// <returns>The tab counter</returns>
        private int GetNewTabCounter() {
            // If we have counters left in the list try getting the smallest one first
            if (m_NewTabCounterList.Count > 0) {
                m_NewTabCounterList.Sort();
                int counter = m_NewTabCounterList[0];
                m_NewTabCounterList.RemoveAt(0);
                return counter;
            } else {
                return m_NewTabCounter++;
            }
        }

        /// <summary>
        /// Checks whether or not only one tab is left and that this tab is empty.
        /// </summary>
        /// <returns>True if only one tab is left and this tab is empty otherwise false</returns>
        private bool IsLastTabNewAndEmpty() {
            if (m_TabControl.Items.Count == 1) {
                ITextEditorTab tab = Tabs.First();
                return tab.MetaData.IsNew && tab.Editor.Text == "";
            } else {
                return false;
            }
        }

        /// <summary>
        /// Callback that gets called when the selection in the tab control changes.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event parameters</param>
        private void OnTabControlSelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (e.Source is TabControl) {
                if (e.AddedItems.Count > 0) {
                    if (e.AddedItems[0] is TabItem tab) {
                        // Update the current tab
                        CurrentTab = tab.Tag as TextEditorTab;

                        SetZoom(m_Zoom);

                        // Invoke event
                        TabChanged?.Invoke();
                    }
                }
            }
        }

        /// <summary>
        /// Callback that gets called on mouse move preview of tab item.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event parameters</param>
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

        /// <summary>
        /// Callback that gets called it a tab item gets dropped.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event parameters</param>
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

        /// <summary>
        /// Callback that gets called when a tab item gives feedback.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event parameters</param>
        private void OnTabItemGiveFeedback(object sender, GiveFeedbackEventArgs e) {
            // We do not want to display the ugly cursors while doing drag and drop
            e.UseDefaultCursors = false;
            e.Handled = true;
        }
    }
}
