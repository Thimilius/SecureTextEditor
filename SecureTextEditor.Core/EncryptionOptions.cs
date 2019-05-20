using Newtonsoft.Json;

namespace SecureTextEditor.Core {
    /// <summary>
    /// Describes what specific algorithm was used to encrypt a file.
    /// </summary>
    public class EncryptionOptions {
        /// <summary>
        /// The general type of algorithm.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public SecurityType Type { get; set; }
        /// <summary>
        /// The type of AES encryption.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public CipherType CipherType { get; set; }
        /// <summary>
        /// The size of the key used in AES encryption.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public int KeySize { get; set; }
        /// <summary>
        /// The block mode used in AES encryption.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public CipherMode AESMode { get; set; }
        /// <summary>
        /// The block padding used in AES encryption.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public CipherPadding AESPadding { get; set; }
    }
}
