using System.Windows.Controls;

namespace SecureTextEditor.GUI.Editor {
    /// <summary>
    /// Interface for a text editor tab implementation.
    /// </summary>
    public interface ITextEditorTab {
        /// <summary>
        /// The tab item control the tab belongs to.
        /// </summary>
        TabItem TabItem { get; }
        /// <summary>
        /// The text editor control the tab belongs to.
        /// </summary>
        TextBox Editor { get; }

        /// <summary>
        /// The metad data for the tab.
        /// </summary>
        TextEditorTabMetaData MetaData { get; set; }

        /// <summary>
        /// Focuses this tab to be the active control.
        /// </summary>
        void Focus();
        /// <summary>
        /// Sets the tab item header.
        /// </summary>
        /// <param name="header">The new header to set</param>
        void SetHeader(string header);
    }
}
