using System.Collections.Generic;
using SecureTextEditor.Crypto.Cipher;
using SecureTextEditor.File;
using SecureTextEditor.File.Options;

namespace SecureTextEditor.GUI.Config {
    /// <summary>
    /// Describes the configuration.
    /// </summary>
    public interface IConfig {
        /// <summary>
        /// Gets or sets the theme.
        /// </summary>
        Theme Theme { get; set; }
        /// <summary>
        /// Gets or sets the zoom level.
        /// </summary>
        int Zoom { get; set; }
        /// <summary>
        /// Gets or sets the text encoding that new files initially have.
        /// </summary>
        TextEncoding NewFileTextEncoding { get; set; }
        /// <summary>
        /// Gets or sets the default cipher type when saving a file.
        /// </summary>
        CipherType DefaultCipherType { get; set; }
        /// <summary>
        /// Gets or sets the default encryption options for the corresponding type.
        /// </summary>
        IDictionary<CipherType, EncryptionOptions> DefaultEncryptionOptions { get; set; }
    }
}
