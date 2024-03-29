﻿using Newtonsoft.Json;
using SecureTextEditor.Crypto.Cipher;
using SecureTextEditor.Crypto.Digest;
using SecureTextEditor.Crypto.Signature;

namespace SecureTextEditor.File.Options {
    /// <summary>
    /// Describes what specific algorithm was used to encrypt a file.
    /// </summary>
    public abstract class EncryptionOptions {
        /// <summary>
        /// The general type of algorithm.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public abstract CipherType CipherType { get; }
        /// <summary>
        /// The cipher key option to use for encryption.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public CipherKeyOption CipherKeyOption { get; set; }
        /// <summary>
        /// The size of the cipher key used in for encryption.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public int CipherKeySize { get; set; }
        /// <summary>
        /// The digest type used for hashing.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public DigestType DigestType { get; set; }
        /// <summary>
        /// The signature type used.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public SignatureType SignatureType { get; set; }
        /// <summary>
        /// The size of the key used for the signature.
        /// </summary>
        [JsonProperty(Required = Required.Always)] public int SignatureKeySize { get; set; }
    }
}
