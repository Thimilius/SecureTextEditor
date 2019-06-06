﻿namespace SecureTextEditor.Crypto {
    /// <summary>
    /// Describes the type of key used in encryption.
    /// </summary>
    public enum KeyType {
        /// <summary>
        /// Describes that a key is to be generated.
        /// </summary>
        Generated,
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
