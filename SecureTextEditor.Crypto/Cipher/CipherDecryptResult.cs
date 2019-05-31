using System;

namespace SecureTextEditor.Crypto.Cipher {
    /// <summary>
    /// Describes the status of the decryption.
    /// </summary>
    public enum CipherDecryptStatus {
        /// <summary>
        /// Describes that the decryption was successfull.
        /// </summary>
        Success,
        /// <summary>
        /// Describes that the decryption failed because the underlying mac reported an error.
        /// </summary>
        MacFailed,
        /// <summary>
        /// Describes that the decryption failed because of an internal error.
        /// </summary>
        Failed
    }

    /// <summary>
    /// Data holder for the result of the decryption operation.
    /// </summary>
    public class CipherDecryptResult {
        /// <summary>
        /// The status of the decryption operation.
        /// </summary>
        public CipherDecryptStatus Status { get; }
        /// <summary>
        /// The underlying exception that was raised (if any).
        /// </summary>
        public Exception Exception { get; }
        /// <summary>
        /// The actual decrypted result of the decryption operation if it was successfull.
        /// </summary>
        public byte[] Result { get; }

        /// <summary>
        /// Creates a new decryption result object with given parameters.
        /// </summary>
        /// <param name="status">The status of the decryption opeation</param>
        /// <param name="exception">The underlying exception that was raised (if any)</param>
        /// <param name="result">The actual result of the decryption operation if it was successfull</param>
        public CipherDecryptResult(CipherDecryptStatus status, Exception exception, byte[] result) {
            Status = status;
            Exception = exception;
            Result = result;
        }
    }
}
