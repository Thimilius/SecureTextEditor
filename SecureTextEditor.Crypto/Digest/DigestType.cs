namespace SecureTextEditor.Crypto.Digest {
    /// <summary>
    /// Describes the digest type.
    /// </summary>
    public enum DigestType {
        /// <summary>
        /// Describes no type.
        /// </summary>
        None,
        /// <summary>
        /// Describes the SHA-256 algorithm.
        /// </summary>
        SHA256,
        /// <summary>
        /// Describes a MAC using AES with the CMAC algotithm.
        /// </summary>
        AESCMAC,
        /// <summary>
        /// Describes the Hash MAC with the SHA-256 algotithm.
        /// </summary>
        HMACSHA256
    }
}
