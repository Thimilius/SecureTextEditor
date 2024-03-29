﻿using Newtonsoft.Json;
using SecureTextEditor.File.Options;

namespace SecureTextEditor.File {
    /// <summary>
    /// Internal data class for abstracting the secure text file.
    /// </summary>
    internal class SecureTextFile {
        /// <summary>
        /// Version number for the secure text file format for compatability reasons.
        /// </summary>
        [JsonProperty(Required = Required.Always)] internal string Version { get; } = "0.1.0";
        /// <summary>
        /// The encoding used for the text.
        /// </summary>
        [JsonProperty(Required = Required.Always)] internal TextEncoding Encoding { get; }
        /// <summary>
        /// The options used for encryption.
        /// </summary>
        [JsonProperty(Required = Required.Always)] internal EncryptionOptions EncryptionOptions { get; }
        /// <summary>
        /// The initilization vector or salt encoded in Base64.
        /// </summary>
        [JsonProperty(Required = Required.Default)] internal string Base64IVOrSalt { get; }
        /// <summary>
        /// The public key used for verifying the signature encoded in Base64.
        /// </summary>
        [JsonProperty(Required = Required.Default)] internal string Base64SignaturePublicKey { get; }
        /// <summary>
        /// The signature of the whole file encoded in Base64.
        /// </summary>
        [JsonProperty(Required = Required.Default)] internal string Base64Signature { get; }
        /// <summary>
        /// The actual cipher encoded in Base64.
        /// </summary>
        [JsonProperty(Required = Required.Always)] internal string Base64Cipher { get; }

        /// <summary>
        /// Creates a new secure text file with given properties.
        /// </summary>
        /// <param name="encoding">The encoding used for the text</param>
        /// <param name="options">The security options used for encryption</param>
        /// <param name="base64Cipher">The actual cipher encoded in Base64</param>
        /// <param name="base64IVOrSalt">The initilization vector encoded in Base64</param>
        /// <param name="base64SignaturePublicKey">The public key used for verifying the signature encoded in Base64</param>
        /// <param name="base64Signature">The sign of the whole file encoded in Base64</param>
        public SecureTextFile(TextEncoding encoding, EncryptionOptions encryptionOptions, string base64Cipher, string base64IVOrSalt, string base64SignaturePublicKey, string base64Signature) {
            Encoding = encoding;
            EncryptionOptions = encryptionOptions;
            Base64Cipher = base64Cipher;
            Base64IVOrSalt = base64IVOrSalt;
            Base64SignaturePublicKey = base64SignaturePublicKey;
            Base64Signature = base64Signature;
        }
    }
}
