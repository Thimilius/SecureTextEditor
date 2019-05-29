using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SecureTextEditor.Crypto;
using SecureTextEditor.Crypto.Cipher;
using SecureTextEditor.Crypto.Digest;
using SecureTextEditor.File;
using SecureTextEditor.File.Options;

namespace SecureTextEditor.GUI.Config {
    /// <summary>
    /// Contains the config used and saved by the application.
    /// </summary>
    public static class AppConfig {
        /// <summary>
        /// Dummy config implementation that makes sure all properties are loaded in.
        /// </summary>
        private class Configuration : IConfig {
            [JsonProperty(Required = Required.Always)] public Theme Theme { get; set; }
            [JsonProperty(Required = Required.Always)] public int Zoom { get; set; }
            [JsonProperty(Required = Required.Always)] public TextEncoding NewFileTextEncoding { get; set; }
            [JsonProperty(Required = Required.Always)] public EncryptionType DefaultEncryptionType { get; set; }
            [JsonProperty(Required = Required.Always)] public IDictionary<EncryptionType, EncryptionOptions> DefaultEncryptionOptions { get; set; }
        }

        /// <summary>
        /// The path to the config file.
        /// </summary>
        private const string FILE_PATH = ".config";
        /// <summary>
        /// Settings for serializing and deserializing the settings file.
        /// </summary>
        private static readonly JsonSerializerSettings SERIALIZER_SETTINGS = new JsonSerializerSettings() {
            Formatting = Formatting.Indented,
            Converters = new List<JsonConverter>() { new StringEnumConverter() }
        };

        /// <summary>
        /// Gets the current config.
        /// </summary>
        public static IConfig Config { get; private set; }

        /// <summary>
        /// Saves the config.
        /// </summary>
        public static void Save() {
            try {
                string json = JsonConvert.SerializeObject(Config, SERIALIZER_SETTINGS);
                System.IO.File.WriteAllText(FILE_PATH, json);
            } catch {
                // Silently fail because error is not important for user
            }
        }

        /// <summary>
        /// Loads the config.
        /// </summary>
        public static void Load() {
            // If a settings file does not exits fallback to default config
            if (!System.IO.File.Exists(FILE_PATH)) {
                SetDefaultSettings();
                return;
            }

            // Try loading the config file and if that fails fallback to default
            try {
                string json = System.IO.File.ReadAllText(FILE_PATH);
                Config = JsonConvert.DeserializeObject<Configuration>(json, SERIALIZER_SETTINGS);
            } catch {
                SetDefaultSettings();
            }
        }

        private static void SetDefaultSettings() {
            // Set default config
            Config = new Configuration() {
                Theme = Theme.DarkMode,
                Zoom = 16,
                NewFileTextEncoding = TextEncoding.UTF8,
                DefaultEncryptionType = EncryptionType.AES,
                DefaultEncryptionOptions = new Dictionary<EncryptionType, EncryptionOptions>() {
                    { EncryptionType.AES, new EncryptionOptionsAES() {
                        DigestType = DigestType.SHA256,
                        KeySize = 192,
                        Mode = CipherMode.CBC,
                        Padding = CipherPadding.PKCS7
                    } },
                    { EncryptionType.RC4, new EncryptionOptionsRC4() {
                        DigestType = DigestType.SHA256,
                        KeySize = 192
                    } },
                }
            };
        }
    }
}
