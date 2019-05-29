using Newtonsoft.Json;
using SecureTextEditor.Crypto.Cipher;

namespace SecureTextEditor.Crypto.Options {
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
