using SecureTextEditor.File.Handler;

namespace SecureTextEditor.GUI.Editor {
    /// <summary>
    /// Stores meta data for a text editor tab.
    /// </summary>
    public class TextEditorTabMetaData {
        /// <summary>
        /// The file meta data associated with the tab.
        /// </summary>
        public FileMetaData FileMetaData { get; set; }
        /// <summary>
        /// Indicates whether or not the file is new.
        /// </summary>
        public bool IsNew { get; set; }
        /// <summary>
        /// Indicates whether or not the file is dirty (has unsaved changes).
        /// </summary>
        public bool IsDirty { get; set; }
    }
}
