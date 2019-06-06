namespace SecureTextEditor.Crypto.Cipher {
    /// <summary>
    /// Describes the type of key used in encryption.
    /// </summary>
    public enum CipherKeyOption {
        /// <summary>
        /// Describes that a key is to be generated.
        /// </summary>
        Generate,
        /// <summary>
        /// Describes that a key is to be generated from a provieded password.
        /// </summary>
        PBE,
        /// <summary>
        /// Describes that a key is to be generated from a provided password with SCRYPT.
        /// </summary>
        PBEWithSCRYPT
    }
}
