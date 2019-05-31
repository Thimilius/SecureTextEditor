using Newtonsoft.Json;
using SecureTextEditor.File.Options;

namespace SecureTextEditor.File {
    /// <summary>
    /// Internal data class for abstracting the secure text file.
    /// </summary>
    internal class SecureTextFile {
        /// <summary>
        /// Version number for the secure text file for compatability reasons.
        /// </summary>
        [JsonProperty(Required = Required.Always)] internal string Version { get; } = "0.1";
        /// <summary>
        /// The options used for encryption.
        /// </summary>
        [JsonProperty(Required = Required.Always)] internal EncryptionOptions EncryptionOptions { get; }
        /// <summary>
        /// The encoding used for the text.
        /// </summary>
        [JsonProperty(Required = Required.Always)] internal TextEncoding Encoding { get; }
        /// <summary>
        /// The initilization vector encoded in Base64.
        /// </summary>
        [JsonProperty(Required = Required.Default)] internal string Base64IV { get; }
        /// <summary>
        /// The actual cipher encoded in Base64.
        /// </summary>
        [JsonProperty(Required = Required.Always)] internal string Base64Cipher { get; }

        /// <summary>
        /// Creates a new secure text file with given properties.
        /// </summary>
        /// <param name="options">The security options used for encryption</param>
        /// <param name="encoding">The encoding used for the text</param>
        /// <param name="base64IV">The initilization vector encoded in Base64</param>
        /// <param name="base64Cipher">The actual cipher encoded in Base64</param>
        internal SecureTextFile(EncryptionOptions encryptionOptions, TextEncoding encoding, string base64IV, string base64Cipher) {
            Encoding = encoding;
            EncryptionOptions = encryptionOptions;
            Base64IV = base64IV;
            Base64Cipher = base64Cipher;
        }
    }
}
