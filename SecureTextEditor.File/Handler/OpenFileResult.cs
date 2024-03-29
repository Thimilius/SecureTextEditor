﻿using System;

namespace SecureTextEditor.File.Handler {
    /// <summary>
    /// Describes the status of an open file operation.
    /// </summary>
    public enum OpenFileStatus {
        /// <summary>
        /// Describes that the open file operation was successfull.
        /// </summary>
        Success,
        /// <summary>
        /// Describes that the open file operation got canceled.
        /// </summary>
        Canceled,
        /// <summary>
        /// Describes that the open file operation failed because of a signature that could not be verifyed.
        /// </summary>
        SignatureFailed,
        /// <summary>
        /// Describes that the open file operation failed because of a MAC that did not match.
        /// </summary>
        MacFailed,
        /// <summary>
        /// Describes that the open file operation generally failed.
        /// </summary>
        Failed
    }

    /// <summary>
    /// Data holder for the result of an open file operation.
    /// </summary>
    public class OpenFileResult {
        /// <summary>
        /// The status of the open file operation.
        /// </summary>
        public OpenFileStatus Status { get; }
        /// <summary>
        /// The underlying exception that got raised (if any).
        /// </summary>
        public Exception Exception { get; }
        /// <summary>
        /// The actual meta data produced by the open file operation if it was successfull.
        /// </summary>
        public FileMetaData FileMetaData { get; }
        /// <summary>
        /// The actual text loaded by the open file operation if it was successfull.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Creates a new open file result object with given parameters.
        /// </summary>
        /// <param name="status">The status of the open file operation</param>
        /// <param name="exception">The underlying exception that got raised (if any)</param>
        /// <param name="fileMetaData">The actual meta data produced by the open file operation if it was successfull</param>
        /// <param name="text">The actual text loaded by the open file operation if it was successfull</param>
        public OpenFileResult(OpenFileStatus status, Exception exception = null, FileMetaData fileMetaData = null, string text = null) {
            Status = status;
            Exception = exception;
            FileMetaData = fileMetaData;
            Text = text;
        }
    }
}
