using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace SecureTextEditor.Crypto {
    /// <summary>
    /// Provides extensions/utilities for security.
    /// </summary>
    public static class SecurityExtensions {
        /// <summary>
        /// Converts a given secure string into a char array which can then be processed by a callback.
        /// </summary>
        /// <param name="src">The secure string to process</param>
        /// <param name="processor">The processor callback</param>
        /// <returns></returns>
        public static void Process(this SecureString src, Action<char[]> processor) {
            byte[] bytes = null;
            char[] chars = null;

            // Pin managed arrays
            GCHandle bytesHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            GCHandle charsHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);

            IntPtr byteStringPointer = IntPtr.Zero;
            try {
                // Convert secure string to unmanaged byte string
                byteStringPointer = Marshal.SecureStringToBSTR(src);

                // Copy unmanaged bytes to managed array
                unsafe {
                    byte* bstrBytes = (byte*)byteStringPointer;
                    // One character is always to bytes in C# (UTF-16)
                    bytes = new byte[src.Length * 2];

                    for (int i = 0; i < bytes.Length; i++) {
                        bytes[i] = *bstrBytes++;
                    }
                }

                // We need to use the UTF-16 encoding to get the characters
                chars = Encoding.Unicode.GetChars(bytes);

                // Invoke the processor
                processor?.Invoke(chars);
            } finally {
                // Clear out the bytes to zero
                if (bytes != null) {
                    for (int i = 0; i < bytes.Length; i++) {
                        bytes[i] = 0;
                    }
                }

                // Clear out the chars to zero
                if (chars != null) {
                    for (int i = 0; i < chars.Length; i++) {
                        chars[i] = '\0';
                    }
                }

                // Make the byte array available for garbage collection again
                bytesHandle.Free();
                charsHandle.Free();

                // Free and clear unmanaged byte string
                if (byteStringPointer != IntPtr.Zero) {
                    Marshal.ZeroFreeBSTR(byteStringPointer);
                }
            }
        }

        /// <summary>
        /// Clears out a byte array to zero.
        /// </summary>
        /// <param name="array">The array to clear out</param>
        public static void Clear(this byte[] array) {
            for (int i = 0; i < array.Length; i++) {
                array[i] = 0;
            }
        }

        /// <summary>
        /// Checks whether or not the content of two given arrays are the same.
        /// </summary>
        /// <param name="a">The first array</param>
        /// <param name="b">The second array</param>
        /// <returns>True if the two arrays have the same content otherwise false</returns>
        public static bool AreEqual(byte[] a, byte[] b) {
            return Org.BouncyCastle.Utilities.Arrays.ConstantTimeAreEqual(a, b);
        }
    }
}
