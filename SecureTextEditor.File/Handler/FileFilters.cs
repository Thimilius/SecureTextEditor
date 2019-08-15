namespace SecureTextEditor.File.Handler {
    /// <summary>
    /// Contains predefined filters for certain file types.
    /// </summary>
    public static class FileFilters {
        /// <summary>
        /// The filter for a secure text file (*.stxt).
        /// </summary>
        public const string STXT_FILE_FILTER       = "Secure Text File (" + FileHandler.STXT_FILE_EXTENSION + ")|*" + FileHandler.STXT_FILE_EXTENSION;
        /// <summary>
        /// The filter for a cipher key file (*.key).
        /// </summary>
        public const string CIPHER_KEY_FILE_FILTER = "Cipher Key File (" + FileHandler.CIPHER_KEY_FILE_EXTENSION + ")|*" + FileHandler.CIPHER_KEY_FILE_EXTENSION;
        /// <summary>
        /// The filter for a MAC key file (*.mackey).
        /// </summary>
        public const string MAC_KEY_FILE_FILTER    = "Mac Key File (" + FileHandler.MAC_KEY_FILE_EXTENSION + ")|*" + FileHandler.MAC_KEY_FILE_EXTENSION;
    }
}
