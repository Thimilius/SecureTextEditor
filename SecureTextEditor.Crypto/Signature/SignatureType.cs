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
        /// Describes the DSA with SHA256 algorithm.
        /// </summary>
        DSAWithSHA256,
        /// <summary>
        /// Describes the ECDSA with SHA256 algorithm.
        /// </summary>
        ECDSAWithSHA256
    }
}
