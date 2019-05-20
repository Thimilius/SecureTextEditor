namespace SecureTextEditor.Core.Cipher {
    /// <summary>
    /// Describes the cipher block mode used when encrypting.
    /// </summary>
    public enum CipherMode {
        /// <summary>
        /// Describes no mode.
        /// </summary>
        None,
        /// <summary>
        /// Describes the Electronic Code Block mode.
        /// </summary>
        ECB,
        /// <summary>
        /// Describes the Cipher Block Chaining mode.
        /// </summary>
        CBC,
        /// <summary>
        /// Describes the Cipher Text Stealing mode.
        /// </summary>
        CTS,
        /// <summary>
        /// Describes the Counter streaming block mode.
        /// </summary>
        CTR,
        /// <summary>
        /// Describes the Cipher FeedBack streaming block mode.
        /// </summary>
        CFB,
        /// <summary>
        /// Describes the Output FeedBack streaming block mode.
        /// </summary>
        OFB
    }
}
