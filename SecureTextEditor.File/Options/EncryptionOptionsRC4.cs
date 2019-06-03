using Newtonsoft.Json;
using SecureTextEditor.Crypto;
using SecureTextEditor.Crypto.Cipher;

namespace SecureTextEditor.File.Options {
    /// <summary>
    /// Describes what specific algorithm was used to encrypt a file with RC4.
    /// </summary>
    public class EncryptionOptionsRC4 : EncryptionOptions {
        /// <summary>
        /// The general type of algorithm which in this case is AES.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public override EncryptionType Type => EncryptionType.RC4;
        /// <summary>
        /// The cipher type used in encryption.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public override CipherType CipherType => CipherType.Stream;
    }
}
