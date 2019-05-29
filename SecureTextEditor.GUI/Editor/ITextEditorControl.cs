using System;
using System.Collections.Generic;
using SecureTextEditor.File;

namespace SecureTextEditor.GUI.Editor {
    public interface ITextEditorControl {
        event Action TabChanged;

        ITextEditorTab CurrentTab { get; }
        IEnumerable<ITextEditorTab> Tabs { get; }

        void NewTab(string content);
        void NewTab(string content, FileMetaData fileMetaData);
        void CloseTab(ITextEditorTab tab);
        void FocusTab(ITextEditorTab tab);

        void ZoomIn();
        void ZoomOut();
        void ZoomReset();

        void NotifyThatTabGotClosed(ITextEditorTab tab);
    }
}
