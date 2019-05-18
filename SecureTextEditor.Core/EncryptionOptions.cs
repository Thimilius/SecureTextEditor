namespace SecureTextEditor.Core {
    /// <summary>
    /// Describes what specific algorithm was used to encrypt a file.
    /// </summary>
    public class EncryptionOptions {
        /// <summary>
        /// The general type of algorithm.
        /// </summary>
        public SecurityType Type { get; set; }
        /// <summary>
        /// The block mode used in AES encryption.
        /// </summary>
        public CipherBlockMode BlockMode { get; set; }
        /// <summary>
        /// The block padding used in AES encryption.
        /// </summary>
        public CipherBlockPadding BlockPadding { get; set; }
    }
}
