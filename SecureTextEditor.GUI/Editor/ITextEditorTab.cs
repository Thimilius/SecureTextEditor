using System.Windows.Controls;
using SecureTextEditor.File;

namespace SecureTextEditor.GUI.Editor {
    public interface ITextEditorTab {
        TabItem TabItem { get; }
        TextBox Editor { get; }

        TextEditorTabMetaData MetaData { get; set; }

        void FocusControls();
        void SetHeader(string header);
    }
}
