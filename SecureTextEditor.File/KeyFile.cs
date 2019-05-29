using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SecureTextEditor.File {
    /// <summary>
    /// Data class for abstracting a key file.
    /// </summary>
    public class KeyFile {
        /// <summary>
        /// The extension used for the file.
        /// </summary>
        public const string FILE_EXTENSION = ".key";

        /// <summary>
        /// The key encoded in Base64.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public string Base64Key { get; }
        /// <summary>
        /// The initilization vector encoded in Base64.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public string Base64IV { get; }

        /// <summary>
        /// Creates a new key file with given properties.
        /// </summary>
        /// <param name="base64Key">The key encoded in Base64</param>
        /// <param name="base64IV">The initilization vector encoded in Base64</param>
        public KeyFile(string base64Key, string base64IV) {
            Base64Key = base64Key;
            Base64IV = base64IV;
        }
    }
}
