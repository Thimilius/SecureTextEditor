using Newtonsoft.Json;
using SecureTextEditor.Crypto.Cipher;
using SecureTextEditor.Crypto.Digest;

namespace SecureTextEditor.Crypto.Options {
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
        /// The digest type used for hashing.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public DigestType DigestType { get; set; }
        /// <summary>
        /// The size of the key used in for encryption.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public int KeySize { get; set; }
    }
}
