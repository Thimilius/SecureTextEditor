using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SecureTextEditor.Crypto.Cipher;
using SecureTextEditor.Crypto.Digest;
using SecureTextEditor.Crypto.Signature;
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
            [JsonProperty(Required = Required.Always)] public CipherType DefaultCipherType { get; set; }
            [JsonProperty(Required = Required.Always)] public IDictionary<CipherType, EncryptionOptions> DefaultEncryptionOptions { get; set; }
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
            TypeNameHandling = TypeNameHandling.Auto,
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
                SetDefaultConfig();
                return;
            }

            // Try loading the config file and if that fails fallback to default
            try {
                string json = System.IO.File.ReadAllText(FILE_PATH);
                Config = JsonConvert.DeserializeObject<Configuration>(json, SERIALIZER_SETTINGS);
            } catch {
                SetDefaultConfig();
            }
        }

        /// <summary>
        /// Sets the configuration to be the default.
        /// </summary>
        private static void SetDefaultConfig() {
            // Set default config
            Config = new Configuration() {
                Theme = Theme.DarkMode,
                Zoom = 16,
                NewFileTextEncoding = TextEncoding.UTF8,
                DefaultCipherType = CipherType.AES,
                DefaultEncryptionOptions = new Dictionary<CipherType, EncryptionOptions>() {
                    { CipherType.AES, new EncryptionOptionsAES() {
                        DigestType = DigestType.SHA256,
                        CipherKeyOption = CipherKeyOption.Generate,
                        CipherKeySize = 192,
                        SignatureType = SignatureType.ECDSAWithSHA256,
                        SignatureKeySize = 1024,
                        AESMode = CipherMode.CBC,
                        AESPadding = CipherPadding.PKCS7
                    } },
                    { CipherType.RC4, new EncryptionOptionsRC4() {
                        DigestType = DigestType.SHA256,
                        CipherKeyOption = CipherKeyOption.Generate,
                        CipherKeySize = 192,
                        SignatureType = SignatureType.DSAWithSHA256,
                        SignatureKeySize = 1024
                    } },
                }
            };
        }
    }
}
