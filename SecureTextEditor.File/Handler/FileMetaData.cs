using SecureTextEditor.File.Options;

namespace SecureTextEditor.File.Handler {
    /// <summary>
    /// Stores meta data for a secure text file.
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
    }
}
