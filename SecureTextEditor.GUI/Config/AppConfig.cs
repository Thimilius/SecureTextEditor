using System.IO;
using Newtonsoft.Json;
using SecureTextEditor.Core;

namespace SecureTextEditor.GUI.Config {
    /// <summary>
    /// Contains the config used and saved by the application.
    /// </summary>
    public static class AppConfig {
        private class Configuration : IConfig {
            public Theme Theme { get; set; }
            public TextEncoding NewFileTextEncoding { get; set; }
        }

        private const string FILE_PATH = ".settings";

        /// <summary>
        /// Gets the current config.
        /// </summary>
        public static IConfig Config { get; private set; }

        /// <summary>
        /// Saves the config.
        /// </summary>
        public static void Save() {
            try {
                string json = JsonConvert.SerializeObject(Config, Formatting.Indented);
                File.WriteAllText(FILE_PATH, json);
            } catch {
                // TODO: Display error feedback
            }
        }

        /// <summary>
        /// Loads the config.
        /// </summary>
        public static void Load() {
            // If a settings file does not exits fallback to default config
            if (!File.Exists(FILE_PATH)) {
                ResetSettings();
            }

            // Try loading the file and fallback to default config should that fail
            try {
                string json = File.ReadAllText(FILE_PATH);
                Config = JsonConvert.DeserializeObject<Configuration>(json);
            } catch {
                // TODO: Display error feedback
                ResetSettings();
            }
        }

        private static void ResetSettings() {
            // Set default config
            Config = new Configuration() {
                Theme = Theme.DarkMode,
                NewFileTextEncoding = TextEncoding.UTF8
            };
        }
    }
}
