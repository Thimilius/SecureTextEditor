namespace SecureTextEditor.Core {
    /// <summary>
    /// Describes the security mechanism used to save a file.
    /// </summary>
    public enum EncryptionType {
        /// <summary>
        /// Describes encryption with the AES algorithm.
        /// </summary>
        AES,
        /// <summary>
        /// Describes encryption with the RC4 algorithm.
        /// </summary>
        RC4
    }
}
