using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SecureTextEditor.Crypto.Options;

namespace SecureTextEditor.Crypto {
    /// <summary>
    /// Data class for abstracting the secure text file.
    /// </summary>
    public class SecureTextFile {
        /// <summary>
        /// The extension used for the file.
        /// </summary>
        public const string FILE_EXTENSION = ".stxt";

        /// <summary>
        /// Version number for the secure text file for compatability reasons.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public string Version { get; } = "0.1";
        /// <summary>
        /// The options used for encryption.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public EncryptionOptions EncryptionOptions { get; }
        /// <summary>
        /// The encoding used for the text.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public TextEncoding Encoding { get; }
        /// <summary>
        /// The digest computed from the cipher encoded in Base64.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public string Base64Digest { get; }
        /// <summary>
        /// The actual cipher encoded in Base64.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public string Base64Cipher { get; }

        /// <summary>
        /// Creates a new secure text file with given properties.
        /// </summary>
        /// <param name="options">The security options used for encryption</param>
        /// <param name="encoding">The encoding used for the text</param>
        /// <param name="base64Digest">The digest computed from the cipher encoded in Base64</param>
        /// <param name="base64Cipher">The actual cipher encoded in Base64</param>
        public SecureTextFile(EncryptionOptions encryptionOptions, TextEncoding encoding, string base64Digest, string base64Cipher) {
            Encoding = encoding;
            EncryptionOptions = encryptionOptions;
            Base64Digest = base64Digest;
            Base64Cipher = base64Cipher;
        }
    }
}
