using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SecureTextEditor.Core {
    /// <summary>
    /// Data class for abstracting a key file.
    /// </summary>
    public class KeyFile {
        /// <summary>
        /// The extension used for the file.
        /// </summary>
        public const string FILE_EXTENSION = ".key";

        /// <summary>
        /// Settings for serializing and deserializing the text file.
        /// </summary>
        private static readonly JsonSerializerSettings SERIALIZER_SETTINGS = new JsonSerializerSettings() {
            Formatting = Formatting.Indented,
            Converters = new List<JsonConverter>() { new StringEnumConverter() }
        };

        /// <summary>
        /// The key encoded in Base64.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public string Base64Key { get; }

        /// <summary>
        /// Creates a new key file with given properties.
        /// </summary>
        /// <param name="base64Key">The key encoded in Base64</param>
        public KeyFile(string base64Key) {
            Base64Key = base64Key;
        }

        /// <summary>
        /// Saves a given secure text file at the given path.
        /// </summary>
        /// <param name="file">The secure text file to save</param>
        /// <param name="path">The path to save the file at</param>
        public static void Save(KeyFile file, string path) {
            string json = JsonConvert.SerializeObject(file, SERIALIZER_SETTINGS);
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// Loads a secure text file at a given path.
        /// </summary>
        /// <param name="path">The path of the file to load</param>
        /// <returns>The loaded secure text file</returns>
        public static KeyFile Load(string path) {
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<KeyFile>(json, SERIALIZER_SETTINGS);
        }
    }
}
