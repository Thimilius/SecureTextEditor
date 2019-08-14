using System.Security;

namespace SecureTextEditor.File.Handler {
    /// <summary>
    /// Handler to resolve the password used in password based encryption.
    /// </summary>
    /// <returns>The password used in password based encryption</returns>
    public delegate SecureString PasswordResolver();
    /// <summary>
    /// Handler to resolve the path to a cipher key file.
    /// </summary>
    /// <param name="keySize">The key size in bits that is to be expected</param>
    /// <returns>The path to the cipher key file to load</returns>
    public delegate string CipherKeyFileResolver(int keySize);
    /// <summary>
    /// Handler to resolve the path to a mac key file.
    /// </summary>
    /// <returns>The path to the mac key file to load</returns>
    public delegate string MacKeyFileResolver();

    /// <summary>
    /// Parameters used for an open file operation.
    /// </summary>
    public class OpenFileParameters {
        /// <summary>
        /// The path to the file to open.
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// The cipher key file resolver.
        /// </summary>
        public CipherKeyFileResolver CipherKeyFileResolver { get; set; }
        /// <summary>
        /// The password resolver.
        /// </summary>
        public PasswordResolver PasswordResolver { get; set; }
        /// <summary>
        /// The mac key file resolver.
        /// </summary>
        public MacKeyFileResolver MacKeyFileResolver { get; set; }
    }
}
