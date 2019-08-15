using System;

namespace SecureTextEditor.Crypto.Storage {
    /// <summary>
    /// Describes the status of the result operation
    /// </summary>
    public enum KeyStorageLoadStatus {
        /// <summary>
        /// Describes the loading was successfull.
        /// </summary>
        Success,
        /// <summary>
        /// Describes that the loading failed because the password of the key storage was wrong.
        /// </summary>
        PasswordWrong,
        /// <summary>
        /// Describes that the loading failed because of an internal error.
        /// </summary>
        Failed
    }

    /// <summary>
    /// Data holder for the result of a key storage loading operation.
    /// </summary>
    public class KeyStorageLoadResult {
        /// <summary>
        /// The status of the loading operation.
        /// </summary>
        public KeyStorageLoadStatus Status { get; }
        /// <summary>
        /// The underlying exception that was raised (if any).
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Creates a new key storage load result with given parameters.
        /// </summary>
        /// <param name="status">The status of the loading operation</param>
        /// <param name="exception">The underlying exception that was raised (if any)</param>
        public KeyStorageLoadResult(KeyStorageLoadStatus status, Exception exception) {
            Status = status;
            Exception = exception;
        }
    }
}
