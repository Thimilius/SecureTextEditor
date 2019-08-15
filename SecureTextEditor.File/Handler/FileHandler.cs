using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SecureTextEditor.Crypto.Cipher;
using SecureTextEditor.File.Options;

namespace SecureTextEditor.File.Handler {
    /// <summary>
    /// Abstract file handler.
    /// </summary>
    public abstract class FileHandler {
        /// <summary>
        /// The extension used for the file.
        /// </summary>
        public const string STXT_FILE_EXTENSION = ".stxt";
        /// <summary>
        /// The extension used for the cipher key file.
        /// </summary>
        public const string CIPHER_KEY_FILE_EXTENSION = ".key";
        /// <summary>
        /// The extension used for the mac key file.
        /// </summary>
        public const string MAC_KEY_FILE_EXTENSION = ".mackey";

        /// <summary>
        /// Settings for serializing and deserializing the text file.
        /// </summary>
        protected static readonly JsonSerializerSettings SERIALIZER_SETTINGS = new JsonSerializerSettings() {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Auto,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter>() { new StringEnumConverter() }
        };
        
        /// <summary>
        /// Creates a new cipher engine based on given encryption options.
        /// </summary>
        /// <param name="options">The encryption options to use</param>
        /// <returns>The created cipher engine</returns>
        protected static CipherEngine CreateCryptoEngine(EncryptionOptions options) {
            if (options is EncryptionOptionsAES optionsAES) {
                return new CipherEngine(optionsAES.CipherType, optionsAES.CipherMode, optionsAES.CipherPadding, options.CipherKeyOption, options.CipherKeySize);
            } else if (options is EncryptionOptionsRC4 optionsRC4) {
                return new CipherEngine(optionsRC4.CipherType, CipherMode.None, CipherPadding.None, options.CipherKeyOption, options.CipherKeySize);
            } else {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Gets the actual encoding object based on given text encoding.
        /// </summary>
        /// <param name="encoding">The text encoding</param>
        /// <returns>The encoding object</returns>
        protected static Encoding GetEncoding(TextEncoding encoding) {
            switch (encoding) {
                case TextEncoding.ASCII: return Encoding.ASCII;
                case TextEncoding.UTF8: return Encoding.UTF8;
                default: throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Constructs the path for the cipher key file.
        /// </summary>
        /// <param name="basePath">The base path to use</param>
        /// <returns>The full path for the cipher key file</returns>
        protected static string ConstructPathForCipherKeyFile(string basePath) {
            return Path.GetFileNameWithoutExtension(basePath) + CIPHER_KEY_FILE_EXTENSION;
        }

        /// <summary>
        /// Constructs the path for the mac key file.
        /// </summary>
        /// <param name="basePath">The base path to use</param>
        /// <returns>The full path for the mac key file</returns>
        protected static string ConstructPathForMacKeyFile(string basePath) {
            return Path.GetFileNameWithoutExtension(basePath) + MAC_KEY_FILE_EXTENSION;
        }
    }
}