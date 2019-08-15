using System.Security;
using SecureTextEditor.File.Options;

namespace SecureTextEditor.File.Handler {
    /// <summary>
    /// Parameters used for a save file operation.
    /// </summary>
    public class SaveFileParameters {
        /// <summary>
        /// The path where the file should be saved
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// The actual text to save.
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// The encoding used to save the text.
        /// </summary>
        public TextEncoding Encoding { get; set; }
        /// <summary>
        /// The encryption options to use.
        /// </summary>
        public EncryptionOptions EncryptionOptions { get; set; }
        /// <summary>
        /// The path to the signature key storage.
        /// </summary>
        public string KeyStoragePath { get; set; }
        /// <summary>
        /// The password for the signature key storage if configured in encryption options.
        /// </summary>
        public SecureString KeyStoragePassword { get; set; }
        /// <summary>
        /// The password used in PBE if configured in encryption options.
        /// </summary>
        public SecureString PBEPassword { get; set; }
    }
}
