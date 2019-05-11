﻿using System.Collections.Generic;
using System.IO;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SecureTextEditor.Core;

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
                Config = JsonConvert.DeserializeObject<Configuration>(json, SERIALIZER_SETTINGS);
            } catch {
                // TODO: Display error feedback
                ResetSettings();
            }
        }

        private static void ResetSettings() {
            // Set default config
            Config = new Configuration() {
                Theme = Theme.DarkMode,
                Zoom = 16,
                NewFileTextEncoding = TextEncoding.UTF8
            };
        }
    }
}
