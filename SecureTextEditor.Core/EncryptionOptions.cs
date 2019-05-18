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
        /// The size of the key used in AES encryption.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public int KeySize { get; set; }
        /// <summary>
        /// The block mode used in AES encryption.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public CipherBlockMode BlockMode { get; set; }
        /// <summary>
        /// The block padding used in AES encryption.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public CipherBlockPadding BlockPadding { get; set; }
    }
}
