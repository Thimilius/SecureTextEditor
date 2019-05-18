using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SecureTextEditor.Core {
    /// <summary>
    /// Data class for abstracting the secure text file.
    /// </summary>
    public class SecureTextFile {
        /// <summary>
        /// The extension used for the file.
        /// </summary>
        public const string FILE_EXTENSION = ".stxt";

        /// <summary>
        /// Settings for serializing and deserializing the text file.
        /// </summary>
        private static readonly JsonSerializerSettings SERIALIZER_SETTINGS = new JsonSerializerSettings() {
            Formatting = Formatting.Indented,
            Converters = new List<JsonConverter>() { new StringEnumConverter() }
        };

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
        /// The actual cipher encoded in Base64.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public string Base64Cipher { get; }

        /// <summary>
        /// Creates a new secure text file with given properties.
        /// </summary>
        /// <param name="options">The security options used for encryption</param>
        /// <param name="encoding">The encoding used for the text</param>
        /// <param name="base64Cipher">The actual cipher encoded in Base64</param>
        public SecureTextFile(EncryptionOptions encryptionOptions, TextEncoding encoding, string base64Cipher) {
            Encoding = encoding;
            EncryptionOptions = encryptionOptions;
            Base64Cipher = base64Cipher;
        }

        /// <summary>
        /// Saves a given secure text file at the given path.
        /// </summary>
        /// <param name="file">The secure text file to save</param>
        /// <param name="path">The path to save the file at</param>
        public static void Save(SecureTextFile file, string path) {
            string json = JsonConvert.SerializeObject(file, SERIALIZER_SETTINGS);
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// Loads a secure text file at a given path.
        /// </summary>
        /// <param name="path">The path of the file to load</param>
        /// <returns>The loaded secure text file</returns>
        public static SecureTextFile Load(string path) {
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<SecureTextFile>(json, SERIALIZER_SETTINGS);
        }
    }
}
