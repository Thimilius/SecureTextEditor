using System;

namespace SecureTextEditor.File.Handler {
    /// <summary>
    /// Describes the status of a save file result.
    /// </summary>
    public enum SaveFileStatus {
        /// <summary>
        /// Describes that the save file operation was successfull.
        /// </summary>
        Success,
        /// <summary>
        /// Describes that the key storage password was wrong.
        /// </summary>
        KeyStoragePasswordWrong,
        /// <summary>
        /// Describes that the save file operation failed.
        /// </summary>
        Failed
    }

    /// <summary>
    /// Data holder for the result of a save file operation.
    /// </summary>
    public class SaveFileResult {
        /// <summary>
        /// The status of the save file operation.
        /// </summary>
        public SaveFileStatus Status { get; }
        /// <summary>
        /// The underlying exception that got raised (if any).
        /// </summary>
        public Exception Exception { get; }
        /// <summary>
        /// The actual new meta data of the save file operation if it was successfull.
        /// </summary>
        public FileMetaData FileMetaData { get; }

        /// <summary>
        /// Creates a new save result object with given parameters.
        /// </summary>
        /// <param name="status">The status of the save file operation</param>
        /// <param name="exception">The underlying exception that got raised (if any)</param>
        /// <param name="fileMetaData">The actual new meta data of the save file operation if it was successfull</param>
        public SaveFileResult(SaveFileStatus status, Exception exception, FileMetaData fileMetaData) {
            Status = status;
            Exception = exception;
            FileMetaData = fileMetaData;
        }
    }
}
