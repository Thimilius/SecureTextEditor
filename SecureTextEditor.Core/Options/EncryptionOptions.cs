using Newtonsoft.Json;
using SecureTextEditor.Core.Cipher;

namespace SecureTextEditor.Core.Options {
    /// <summary>
    /// Describes what specific algorithm was used to encrypt a file.
    /// </summary>
    public abstract class EncryptionOptions {
        /// <summary>
        /// The general type of algorithm.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public abstract EncryptionType Type { get; }
        /// <summary>
        /// The cipher type used in encryption.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public abstract CipherType CipherType { get; }
        /// <summary>
        /// The size of the key used in for encryption.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public int KeySize { get; set; }
    }
}
