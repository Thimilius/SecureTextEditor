using System;
using System.ComponentModel;
using System.Linq;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using AdonisUI;
using Microsoft.Win32;
using SecureTextEditor.File;
using SecureTextEditor.File.Handler;
using SecureTextEditor.GUI.Config;
using SecureTextEditor.GUI.Dialog;
using SecureTextEditor.GUI.Editor;

namespace SecureTextEditor.GUI {
    /// <summary>
    /// Interaction logic for the main window.
    /// </summary>
    public partial class EditorWindow : Window {
        /// <summary>
        /// Gets the text editor control.
        /// </summary>
        public ITextEditorControl TextEditorControl { get; private set; }

        /// <summary>
        /// The file handler for opening files.
        /// </summary>
        private readonly OpenFileHandler m_OpenFileHandler;

        /// <summary>
        /// Creates a new main window.
        /// </summary>
        public EditorWindow() { 
            InitializeComponent();

            m_OpenFileHandler = new OpenFileHandler();

            // We need to set the inital theme based on config
            ChangeTheme(AppConfig.Config.Theme);

            // Create text editor
            TextEditorControl = new TextEditorControl(this, EditorTabControl);
            TextEditorControl.TabChanged += UpdateUI;

            // Open an inital empty tab
            TextEditorControl.NewTab("");

            // Subscribe to global events
            Closing += OnWindowClosing;
            ThemeCheckBoxLightMode.Click += (s, e) => ChangeTheme(Theme.LightMode);
            ThemeCheckBoxDarkMode.Click += (s, e) => ChangeTheme(Theme.DarkMode);
            EncodingCheckBoxASCII.Click += (s, e) => ChangeEncoding(TextEncoding.ASCII);
            EncodingCheckBoxUTF8.Click += (s, e) => ChangeEncoding(TextEncoding.UTF8);
        }

        /// <summary>
        /// Prompts the dialog window for saving.
        /// </summary>
        /// <param name="tab">The tab that should get saved</param>
        public void PromptSaveDialog(ITextEditorTab tab) {
            // Show question dialog
            bool save = DialogBox.Show(this,
                $"Do you want to save \"{tab.MetaData.FileMetaData.FileName}\" before closing?",
                "Save",
                DialogBoxButton.YesNo,
                DialogBoxIcon.Question);

            if (save) {
                PromptSaveWindow(tab);
            }
        }

        /// <summary>
        /// Prompts the save dialogs for all unsaved tabs.
        /// </summary>
        private void PromptSaveDialogs() {
            // Loop through every dirty tab
            foreach (var tab in TextEditorControl.Tabs.Where(t => t.MetaData.IsDirty)) {
                // Focus the tab and prompt save dialog for it
                TextEditorControl.SelectTab(tab);
                PromptSaveDialog(tab);
            }
        }

        /// <summary>
        /// Prompts the save window for a given tab.
        /// </summary>
        /// <param name="tab">The tab to prompt the save window for</param>
        private void PromptSaveWindow(ITextEditorTab tab) {
            // Open and show the save dialog
            Window window = new SaveWindow(TextEditorControl, tab) {
                Owner = this,
            };
            window.ShowDialog();

            UpdateUI();
        }

        /// <summary>
        /// Opens a file at a given path or opens up an file dialog for it.
        /// </summary>
        /// <param name="path">The path to a file to open or null if a dialog should be displyed</param>
        public void OpenFile(string path) {
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
            OpenFileParameters parameters = new OpenFileParameters() {
                Path = path,
                CipherKeyFileResolver = CipherKeyFileResolver,
                PBEPasswordResolver = PBEPasswordResolver,
                MacKeyFileResolver = MacKeyFileResolver
            };
            OpenFileResult result = m_OpenFileHandler.OpenFile(parameters);

            if (result.Status == OpenFileStatus.Success) {
                // Open new tab for the file
                TextEditorControl.NewTab(result.Text, new TextEditorTabMetaData() {
                    FileMetaData = result.FileMetaData,
                    IsNew = false,
                    IsDirty = false
                });

                // Update UI
                UpdateEncodingStatus();
            } else if (result.Status == OpenFileStatus.SignatureFailed) {
                DialogBox.Show(
                    Application.Current.MainWindow,
                    "It appears the file can not be restored correctly!\nThis can be an indication that the file got tampered with!",
                    "Signature Failed",
                    DialogBoxButton.OK,
                    DialogBoxIcon.Error
                );
            } else if (result.Status == OpenFileStatus.MacFailed) {
                DialogBox.Show(
                    Application.Current.MainWindow,
                    "It appears the file can not be restored correctly!\nThis can be an indication that the file got tampered with!",
                    "Mac Failed",
                    DialogBoxButton.OK,
                    DialogBoxIcon.Error
                );
            } else if (result.Status == OpenFileStatus.Failed) {
                DialogBox.Show(
                    Application.Current.MainWindow,
                    $"Failed to open/decrypt the file!",
                    "Opening Failed",
                    DialogBoxButton.OK,
                    DialogBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// Resolver for the PBE password.
        /// </summary>
        /// <returns>The PBE password</returns>
        private SecureString PBEPasswordResolver() {
            PasswordWindow window = new PasswordWindow(this, "You need to provide a password to open the file!");
            bool? result = window.ShowDialog();

            if (result.Value) {
                SecureString password = window.Password;
                window.Clear();
                return password;
            } else {
                return null;
            }
        }

        /// <summary>
        /// Resolver for the cipher key file path.
        /// </summary>
        /// <param name="keySize">The wanted key size</param>
        /// <returns>The path to the key file</returns>
        private string CipherKeyFileResolver(int keySize) {
            bool IsKeyFileValid(string pathToKeyFile, int expectedKeySize) {
                // We simply check if the size of the file that got selected matches the key size we expect
                System.IO.FileInfo info = new System.IO.FileInfo(pathToKeyFile);
                return info.Length == expectedKeySize;
            }

            int keySizeInBytes = keySize / 8;
            // Prompt an initial dialog for the key file
            string keyFilePath = ShowFileDialogForKeyFile(
                "The file you want to open requires a cipher key file to decrypt!",
                "Cipher Key File Required",
                "Open Cipher Key File",
                FileFilters.CIPHER_KEY_FILE_FILTER);
            if (keyFilePath == null) {
                return null;
            }
            bool keyFileValid = IsKeyFileValid(keyFilePath, keySizeInBytes);

            // Prompt the dialog for as long as the key file is not valid
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
        }

        /// <summary>
        /// Resolver for the MAC key file path.
        /// </summary>
        /// <returns>The path to the MAC key file path</returns>
        private string MacKeyFileResolver() {
            return ShowFileDialogForKeyFile(
                "The file you want to open requires a mac key file to decrypt!",
                "Mac Key File Required",
                "Open Mac Key File",
                FileFilters.MAC_KEY_FILE_FILTER
            );
        }

        /// <summary>
        /// Shows the dialog box and file dialog for a key file.
        /// </summary>
        /// <param name="message">The message of the dialog box</param>
        /// <param name="messageTitle">The title of the dialog box</param>
        /// <param name="dialogTitle">The title of the file dialog</param>
        /// <param name="dialogFilter">The filter of the file dialog</param>
        /// <returns>The path to the key file</returns>
        private string ShowFileDialogForKeyFile(string message, string messageTitle, string dialogTitle, string dialogFilter) {
            DialogBox.Show(
                    Application.Current.MainWindow,
                    message,
                    messageTitle,
                    DialogBoxButton.OK,
                    DialogBoxIcon.Key
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

        /// <summary>
        /// Callback when the user wants to exit the application.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event parameters</param>
        private void OnExit(object sender, EventArgs e) {
            Close();
        }

        /// <summary>
        /// Callback when the user wants to create a new tab.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event parameters</param>
        private void OnNew(object sender, RoutedEventArgs e) {
            TextEditorControl.NewTab("");
        }

        /// <summary>
        /// Callback when the user wants to open a file.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event parameters</param>
        private void OnOpen(object sender, RoutedEventArgs e) {
            OpenFile(null);
        }

        /// <summary>
        /// Callback when the user wants to save the current tab.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event parameters</param>
        private void OnSave(object sender, RoutedEventArgs e) {
            PromptSaveWindow(TextEditorControl.CurrentTab);
        }

        /// <summary>
        /// Callback when the window gets closed.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event parameters</param>
        private void OnWindowClosing(object sender, CancelEventArgs e) {
            PromptSaveDialogs();
        }

        /// <summary>
        /// Callback when the user wants to zoom in.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event parameters</param>
        private void OnZoomIn(object sender, RoutedEventArgs e) {
            TextEditorControl.ZoomIn();
        }

        /// <summary>
        /// Callback when the user wants to zoom out.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event parameters</param>
        private void OnZoomOut(object sender, RoutedEventArgs e) {
            TextEditorControl.ZoomOut();
        }

        /// <summary>
        /// Callback when the user wants to reset the zoom.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event parameters</param>
        private void OnZoomReset(object sender, RoutedEventArgs e) {
            TextEditorControl.ZoomReset();
        }

        /// <summary>
        /// Callback when the user wants to close a tab.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event parameters</param>
        private void OnCloseTab(object sender, RoutedEventArgs e) {
            TextEditorControl.CloseTab(TextEditorControl.CurrentTab);
        }

        /// <summary>
        /// Callback that gets executed when the encoding changes.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event parameters</param>
        private void OnEncodingChanged(object sender, RoutedEventArgs e) {
            // Update encoding checkboxes
            EncodingCheckBoxASCII.IsChecked = TextEditorControl.CurrentTab.MetaData.FileMetaData.Encoding == TextEncoding.ASCII;
            EncodingCheckBoxUTF8.IsChecked = TextEditorControl.CurrentTab.MetaData.FileMetaData.Encoding == TextEncoding.UTF8;
        }

        /// <summary>
        /// Changes the encoding for the current tab.
        /// </summary>
        /// <param name="encoding">The encodig to change to</param>
        private void ChangeEncoding(TextEncoding encoding) {
            TextEditorControl.CurrentTab.MetaData.FileMetaData.Encoding = encoding;

            UpdateEncodingStatus();
        }

        /// <summary>
        /// Changes the theme.
        /// </summary>
        /// <param name="theme">The theme to change to</param>
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

        /// <summary>
        /// Updates all ui elements of the main window.
        /// </summary>
        public void UpdateUI() {
            UpdateEncodingStatus();
            UpdateEditorStatus();
            UpdateWindowTitle();
        }

        /// <summary>
        /// Updates the encoding status.
        /// </summary>
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

        /// <summary>
        /// Updates the editor status.
        /// </summary>
        private void UpdateEditorStatus() {
            TextBox editor = TextEditorControl.CurrentTab.Editor;

            // Compute the values from the editor
            int caretIndex = editor.CaretIndex;
            int lineIndex = Math.Max(0, editor.GetLineIndexFromCharacterIndex(caretIndex));
            int charIndex = Math.Max(0, editor.GetCharacterIndexFromLineIndex(lineIndex));

            // Update the actual labels and take into account an empty text editor
            LinesLabel.Text = $"Lines: {Math.Max(1, editor.LineCount)}";
            LineLabel.Text = $"Ln: {lineIndex + 1}";
            ColumnLabel.Text = $"Col: {(caretIndex - charIndex) + 1}";
            SelectionLabel.Text = $"Sel: {editor.SelectionLength}";
        }

        /// <summary>
        /// Updates the window title.
        /// </summary>
        private void UpdateWindowTitle() {
            Title = $"Secure Text Editor - {TextEditorControl.CurrentTab.MetaData.FileMetaData.FilePath}";
        }
    }
}
