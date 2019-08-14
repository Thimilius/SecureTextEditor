using SecureTextEditor.File.Options;
using System.Security;
using System.Threading.Tasks;

namespace SecureTextEditor.File.Handler {
    /// <summary>
    /// Handler to resolve the password used in password based encryption.
    /// </summary>
    /// <returns>The password used in password based encryption</returns>
    public delegate SecureString PasswordResolver();
    /// <summary>
    /// Handler to resolve the path to a cipher key file.
    /// </summary>
    /// <param name="keySize">The key size that is to be expected</param>
    /// <returns>The path to the cipher key file to load</returns>
    public delegate string CipherKeyFileResolver(int keySize);
    /// <summary>
    /// Handler to resolve the path to a mac key file.
    /// </summary>
    /// <returns>The path to the mac key file to load</returns>
    public delegate string MacKeyFileResolver();

    public interface IFileHandler {
        /// <summary>
        /// Saves a file with given parameters asynchronously.
        /// </summary>
        /// <param name="path">The path where the file should be saved</param>
        /// <param name="text">The actual text to save</param>
        /// <param name="encoding">The encoding for the text</param>
        /// <param name="options">The encryption options to use when saving</param>
        /// <param name="password">The password to use in PBE if any</param>
        /// <returns>The result of the save operation</returns>
        Task<SaveFileResult> SaveFileAsync(string path, string text, TextEncoding encoding, EncryptionOptions options, SecureString password);
        /// <summary>
        /// Opens a file with given parameters.
        /// </summary>
        /// <param name="path">The path to the file to open</param>
        /// <param name="passwordResolver">The password resolver</param>
        /// <param name="cipherKeyFileResolver"></param>
        /// <param name="macKeyFileResolver"></param>
        /// <returns>The result of the open operation</returns>
        OpenFileResult OpenFile(string path, PasswordResolver passwordResolver, CipherKeyFileResolver cipherKeyFileResolver, MacKeyFileResolver macKeyFileResolver);
    }
}
