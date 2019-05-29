using SecureTextEditor.File.Options;

namespace SecureTextEditor.File {
    // TODO: Move IsNew and ÍsDirty outside in tab meta data

    /// <summary>
    /// Stores meta data for a file represented in a tab.
    /// </summary>
    public class FileMetaData {
        /// <summary>
        /// The encoding to use for the text.
        /// </summary>
        public TextEncoding Encoding { get; set; }
        /// <summary>
        /// The encryption options used. Will be null if the file is new.
        /// </summary>
        public EncryptionOptions EncryptionOptions { get; set; }
        /// <summary>
        /// The name of the file including the extension.
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// The full path to the file.
        /// </summary>
        public string FilePath { get; set; }
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
