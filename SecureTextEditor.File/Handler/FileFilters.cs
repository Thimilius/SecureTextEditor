namespace SecureTextEditor.File.Handler {
    /// <summary>
    /// Contains predefined filters for certain file types.
    /// </summary>
    public static class FileFilters {
        /// <summary>
        /// The filter for a secure text file (*.stxt).
        /// </summary>
        public const string STXT_FILE_FILTER       = "Secure Text File (" + SecureTextFileHandler.STXT_FILE_EXTENSION + ")|*" + SecureTextFileHandler.STXT_FILE_EXTENSION;
        /// <summary>
        /// The filter for a cipher key file (*.key).
        /// </summary>
        public const string CIPHER_KEY_FILE_FILTER = "Cipher Key File (" + SecureTextFileHandler.CIPHER_KEY_FILE_EXTENSION + ")|*" + SecureTextFileHandler.CIPHER_KEY_FILE_EXTENSION;
        /// <summary>
        /// The filter for a mac key file (*.mackey).
        /// </summary>
        public const string MAC_KEY_FILE_FILTER    = "Mac Key File (" + SecureTextFileHandler.MAC_KEY_FILE_EXTENSION + ")|*" + SecureTextFileHandler.MAC_KEY_FILE_EXTENSION;
    }
}
