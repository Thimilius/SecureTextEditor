using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace SecureTextEditor.Core {
    public class SecureTextFile {
        public const string FILE_EXTENSION = ".stxt";

        public TextEncoding Encoding { get; }
        public CipherMode Mode { get; }
        public CipherPadding Padding { get; }
        public string Base64Cipher { get; }

        public SecureTextFile(TextEncoding encoding, CipherMode mode, CipherPadding padding, string base64Cipher) {
            Encoding = encoding;
            Mode = mode;
            Padding = padding;
            Base64Cipher = base64Cipher;
        }

        public static void Save(SecureTextFile file, string path) {
            string json = JsonConvert.SerializeObject(file, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        public static SecureTextFile Load(string path) {
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<SecureTextFile>(json);
        }
    }
}
