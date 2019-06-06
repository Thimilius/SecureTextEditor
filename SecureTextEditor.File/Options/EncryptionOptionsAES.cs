using Newtonsoft.Json;
using SecureTextEditor.Crypto.Cipher;

namespace SecureTextEditor.File.Options {
    /// <summary>
    /// Describes what specific algorithm was used to encrypt a file with AES.
    /// </summary>
    public class EncryptionOptionsAES : EncryptionOptions {
        /// <summary>
        /// The cipher type used in encryption.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public override CipherType Type => CipherType.AES;
        /// <summary>
        /// The block mode used in AES encryption.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public CipherMode Mode { get; set; }
        /// <summary>
        /// The block padding used in AES encryption.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public CipherPadding Padding { get; set; }
    }
}
