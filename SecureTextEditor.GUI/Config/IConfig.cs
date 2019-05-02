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
        /// Gets or sets the text encoding that new files initially have.
        /// </summary>
        TextEncoding NewFileTextEncoding { get; set; }
    }
}
