using SecureTextEditor.Core;

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
        /// Gets or sets the default options when saving a file.
        /// </summary>
        EncryptionOptions DefaultSaveOptions { get; set; }
    }
}
