using System.Threading.Tasks;

namespace SecureTextEditor.File.Handler {
    public interface IFileHandler {
        /// <summary>
        /// Saves a file with given parameters asynchronously.
        /// </summary>
        /// <param name="parameters">The save file parameters to use</param>
        /// <returns>The result of the save operation</returns>
        Task<SaveFileResult> SaveFileAsync(SaveFileParameters parameters);
        /// <summary>
        /// Opens a file with given parameters.
        /// </summary>
        /// <param name="parameters">The open file parameters to use</param>
        /// <returns>The result of the open operation</returns>
        OpenFileResult OpenFile(OpenFileParameters parameters);
    }
}
