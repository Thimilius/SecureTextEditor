using System;
using System.Collections.Generic;

namespace SecureTextEditor.GUI.Editor {
    /// <summary>
    /// Interface for a text editor control.
    /// </summary>
    public interface ITextEditorControl {
        /// <summary>
        /// Event that occurs when a tab changes.
        /// </summary>
        event Action TabChanged;

        /// <summary>
        /// The current selectd tab.
        /// </summary>
        ITextEditorTab CurrentTab { get; }
        /// <summary>
        /// The collection of all open tabs.
        /// </summary>
        IEnumerable<ITextEditorTab> Tabs { get; }

        /// <summary>
        /// Opens a new tab with the given content and default meta data.
        /// </summary>
        /// <param name="content">The text content the tab will be filled with</param>
        void NewTab(string content);
        /// <summary>
        /// Opens a new tab with the given content and meta data.
        /// </summary>
        /// <param name="content">The text content the tab will be filled with</param>
        /// <param name="metaData">The tab meta data</param>
        void NewTab(string content, TextEditorTabMetaData metaData);
        /// <summary>
        /// Closes a given tab.
        /// </summary>
        /// <param name="tab">The tab to close</param>
        void CloseTab(ITextEditorTab tab);
        /// <summary>
        /// Selects a given tab.
        /// </summary>
        /// <param name="tab">The tab to focus</param>
        void SelectTab(ITextEditorTab tab);

        /// <summary>
        /// Zooms in the text editor of the current selected tab.
        /// </summary>
        void ZoomIn();
        /// <summary>
        /// Zooms out the text editor of the current selected tab.
        /// </summary>
        void ZoomOut();
        /// <summary>
        /// Resets the zoom of the text editor of the current selected tab.
        /// </summary>
        void ZoomReset();

        /// <summary>
        /// Notifies the text editor control that the given tab got closed.
        /// </summary>
        /// <param name="tab">The tab that got saved</param>
        void NotifyThatTabGotSaved(ITextEditorTab tab);
    }
}
