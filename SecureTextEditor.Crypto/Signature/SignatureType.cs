namespace SecureTextEditor.Crypto.Signature {
    /// <summary>
    /// Describes the type of signature algorithm.
    /// </summary>
    public enum SignatureType {
        /// <summary>
        /// Describes that no signature is used.
        /// </summary>
        None,
        /// <summary>
        /// Describes the SHA256 with DSA algorithm.
        /// </summary>
        SHA256WithDSA
    }
}
