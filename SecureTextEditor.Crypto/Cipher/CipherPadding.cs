namespace SecureTextEditor.Crypto.Cipher {
    /// <summary>
    /// Describes the cipher padding used when encrypting.
    /// </summary>
    public enum CipherPadding {
        /// <summary>
        /// Describes that no padding is used.
        /// </summary>
        None,
        /// <summary>
        /// Describes the ISO107816-4 padding.
        /// </summary>
        ISO7816d4,
        /// <summary>
        /// Describes the ISO10126-2 padding.
        /// </summary>
        ISO10126d2,
        /// <summary>
        /// Describes the PKCS#7 padding.
        /// </summary>
        PKCS7,
        /// <summary>
        /// Describes the trailing bit complement padding.
        /// </summary>
        TCB,
        /// <summary>
        /// Describes the X9.23 padding.
        /// </summary>
        X923,
        /// <summary>
        /// Describes the Zero Byte padding.
        /// </summary>
        ZeroBytes,
    }
}
