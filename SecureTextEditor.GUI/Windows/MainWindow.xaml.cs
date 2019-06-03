using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AdonisUI;
using Microsoft.Win32;
using SecureTextEditor.File;
using SecureTextEditor.File.Handler;
using SecureTextEditor.GUI.Config;
using SecureTextEditor.GUI.Editor;

namespace SecureTextEditor.GUI {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public ITextEditorControl TextEditorControl { get; private set; }

        public MainWindow() { 
            InitializeComponent();

            // We need to set the inital theme based on config
            ChangeTheme(AppConfig.Config.Theme);

            // Create text editor
            TextEditorControl = new TextEditorControl(this, EditorTabControl);
            TextEditorControl.TabChanged += UpdateUI;

            // Open an inital empty tab
            TextEditorControl.NewTab("");

            // Subscribe to global events
            EditorTabControl.MouseWheel += OnZoomChanged;
            Closing += OnWindowClosing;
            ThemeCheckBoxLightMode.Click += (s, e) => ChangeTheme(Theme.LightMode);
            ThemeCheckBoxDarkMode.Click += (s, e) => ChangeTheme(Theme.DarkMode);
            EncodingCheckBoxASCII.Click += (s, e) => ChangeEncoding(TextEncoding.ASCII);
            EncodingCheckBoxUTF8.Click += (s, e) => ChangeEncoding(TextEncoding.UTF8);
        }

        public void OpenFile(string path) {
            string ShowFileDialogForKeyFile(string message, string messageTitle, string dialogTitle, string dialogFilter) {
                DialogWindow.Show(
                        Application.Current.MainWindow,
                        message,
                        messageTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                // Show dialog for opening a file
                var dialog = new OpenFileDialog {
                    Title = dialogTitle,
                    Filter = dialogFilter
                };
                bool? keyFileResult = dialog.ShowDialog();

                if (keyFileResult == false) {
                    return null;
                } else {
                    return dialog.FileName;
                }
            }

            // Check if we need to show the open file dialog first
            if (path == null) {
                // Show dialog for opening a file
                var dialog = new OpenFileDialog {
                    Title = "Open Secure Text File",
                    Filter = FileFilters.STXT_FILE_FILTER
                };
                bool? openFileResult = dialog.ShowDialog();

                path = dialog.FileName;

                // Check if the file is already open
                bool fileAlreadyOpen = false;
                var tabs = TextEditorControl.Tabs.Where(t => t.MetaData.FileMetaData.FilePath == path);
                if (tabs.Any()) {
                    TextEditorControl.SelectTab(tabs.First());
                    fileAlreadyOpen = true;
                }

                // If no file for opening was selected or the file is already open we can bail out
                if (openFileResult == false || fileAlreadyOpen) {
                    return;
                }
            }

            // Open actual file
            OpenFileResult result = FileHandler.OpenFile(path,
                keySize => {
                    bool IsKeyFileValid(string pathToKeyFile, int expectedKeySize) {
                        System.IO.FileInfo info = new System.IO.FileInfo(pathToKeyFile);
                        return info.Length == expectedKeySize;
                    }
                     
                    int keySizeInBytes = keySize / 8;
                    string keyFilePath = ShowFileDialogForKeyFile(
                        "The file you want to open requires a cipher key file to decrypt!",
                        "Cipher Key File Required",
                        "Open Cipher Key File",
                        FileFilters.CIPHER_KEY_FILE_FILTER);
                    if (keyFilePath == null) {
                        return null;
                    }
                    bool keyFileValid = IsKeyFileValid(keyFilePath, keySizeInBytes);

                    while (!keyFileValid) {
                        keyFilePath = ShowFileDialogForKeyFile(
                            $"The key file you selected does not match the required size of {keySizeInBytes} bytes!\nPlease select a new one.",
                            "Cipher Key File Length Wrong",
                            "Open Cipher Key File",
                            FileFilters.CIPHER_KEY_FILE_FILTER);
                        if (keyFilePath == null) {
                            return null;
                        }
                        keyFileValid = IsKeyFileValid(keyFilePath, keySizeInBytes);
                    }

                    return keyFilePath;
                },
                () => ShowFileDialogForKeyFile(
                    "The file you want to open requires a mac key file to decrypt!",
                    "Mac Key File Required",
                    "Open Mac Key File",
                    FileFilters.MAC_KEY_FILE_FILTER
                )
            );

            if (result.Status == OpenFileStatus.Success) {
                // Open new tab for the file
                TextEditorControl.NewTab(result.Text, new TextEditorTabMetaData() {
                    FileMetaData = result.FileMetaData,
                    IsNew = false,
                    IsDirty = false
                });

                // Update UI
                UpdateEncodingStatus();
            } else if (result.Status == OpenFileStatus.MacFailed) {
                DialogWindow.Show(
                    Application.Current.MainWindow,
                    "It appears the file can not be restored correctly!\nThis can be an indication that the file got tampered with!",
                    "File Broken",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            } else if (result.Status == OpenFileStatus.Failed) {
                DialogWindow.Show(
                    Application.Current.MainWindow,
                    $"Failed to open the file:\n{path}\n{result.Exception.GetType()}\n{result.Exception.Message}",
                    "Opening Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        public void UpdateUI() {
            UpdateEncodingStatus();
            UpdateEditorStatus();
            UpdateWindowTitle();
        }

        public void PromptSaveDialog(ITextEditorTab tab) {
            // Show question dialog
            bool save = DialogWindow.Show(this,
                $"Do you want to save \"{tab.MetaData.FileMetaData.FileName}\" before closing?",
                "Save",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (save) {
                PromptSaveWindow(tab);
            }
        }

        private void PromptSaveDialogs() {
            // Loop through every dirty tab
            foreach (var tab in TextEditorControl.Tabs.Where(t => t.MetaData.IsDirty)) {
                // Focus the tab and prompt save dialog for it
                TextEditorControl.SelectTab(tab);
                PromptSaveDialog(tab);
            }
        }

        private void PromptSaveWindow(ITextEditorTab tab) {
            // Open and show the save dialog
            Window window = new SaveWindow(TextEditorControl, tab) {
                Owner = this,
            };
            window.ShowDialog();

            UpdateUI();
        }

        private void OnExit(object sender, EventArgs e) {
            Close();
        }

        private void OnNew(object sender, RoutedEventArgs e) {
            TextEditorControl.NewTab("");
        }

        private void OnOpen(object sender, RoutedEventArgs e) {
            OpenFile(null);
        }

        private void OnSave(object sender, RoutedEventArgs e) {
            PromptSaveWindow(TextEditorControl.CurrentTab);
        }

        private void OnWindowClosing(object sender, CancelEventArgs e) {
            PromptSaveDialogs();
        }

        private void OnZoomIn(object sender, RoutedEventArgs e) {
            TextEditorControl.ZoomIn();
        }

        private void OnZoomOut(object sender, RoutedEventArgs e) {
            TextEditorControl.ZoomOut();
        }

        private void OnZoomReset(object sender, RoutedEventArgs e) {
            TextEditorControl.ZoomReset();
        }

        private void OnZoomChanged(object sender, MouseWheelEventArgs e) {
            // Handles zoom change via mouse wheel when control is held down
            if (Keyboard.PrimaryDevice.Modifiers == ModifierKeys.Control) {
                if (e.Delta > 0) {
                    TextEditorControl.ZoomIn();
                } else {
                    TextEditorControl.ZoomOut();
                }
            }
        }

        private void OnCloseTab(object sender, RoutedEventArgs e) {
            TextEditorControl.CloseTab(TextEditorControl.CurrentTab);
        }

        private void OnEncodingChanged(object sender, RoutedEventArgs e) {
            // Update encoding checkboxes
            EncodingCheckBoxASCII.IsChecked = TextEditorControl.CurrentTab.MetaData.FileMetaData.Encoding == TextEncoding.ASCII;
            EncodingCheckBoxUTF8.IsChecked = TextEditorControl.CurrentTab.MetaData.FileMetaData.Encoding == TextEncoding.UTF8;
        }

        private void ChangeEncoding(TextEncoding encoding) {
            TextEditorControl.CurrentTab.MetaData.FileMetaData.Encoding = encoding;

            UpdateEncodingStatus();
        }

        private void ChangeTheme(Theme theme) {
            // Store theme in config
            AppConfig.Config.Theme = theme;

            // Make actual visual change
            Uri locator = theme == Theme.LightMode ? ResourceLocator.LightColorScheme : ResourceLocator.DarkColorScheme;
            ResourceLocator.SetColorScheme(Application.Current.Resources, locator);

            // Update theme checkboxes
            ThemeCheckBoxLightMode.IsChecked = theme == Theme.LightMode;
            ThemeCheckBoxDarkMode.IsChecked = theme == Theme.DarkMode;
        }

        private void UpdateEncodingStatus() {
            string TextEncodingToString(TextEncoding textEncoding) {
                switch (textEncoding) {
                    case TextEncoding.ASCII: return "ASCII";
                    case TextEncoding.UTF8: return "UTF-8";
                    default: return "Unknown";
                }
            }

            // Update encoding checkboxes
            var encoding = TextEditorControl.CurrentTab.MetaData.FileMetaData.Encoding;
            EncodingCheckBoxASCII.IsChecked = encoding == TextEncoding.ASCII;
            EncodingCheckBoxUTF8.IsChecked = encoding == TextEncoding.UTF8;

            EncodingLabel.Text = $"Encoding: {TextEncodingToString(encoding)}";
        }

        private void UpdateEditorStatus() {
            // Update status bar texts and take into account empty text editor
            TextBox editor = TextEditorControl.CurrentTab.Editor;
            LinesLabel.Text = $"Lines: {Math.Max(1, editor.LineCount)}";
            int caretIndex = editor.CaretIndex;
            int lineIndex = Math.Max(0, editor.GetLineIndexFromCharacterIndex(caretIndex));
            LineLabel.Text = $"Ln: {lineIndex + 1}";
            int charIndex = Math.Max(0, editor.GetCharacterIndexFromLineIndex(lineIndex));
            ColumnLabel.Text = $"Col: {(caretIndex - charIndex) + 1}";
            SelectionLabel.Text = $"Sel: {editor.SelectionLength}";
        }

        private void UpdateWindowTitle() {
            Title = $"Secure Text Editor - {TextEditorControl.CurrentTab.MetaData.FileMetaData.FilePath}";
        }
    }
}
