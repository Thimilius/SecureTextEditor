using System.Windows;
using SecureTextEditor.GUI.Config;

namespace SecureTextEditor.GUI {
    /// <summary>
    /// Startup logic for the actual Application.
    /// </summary>
    public partial class App : Application {
        private void OnStart(object sender, StartupEventArgs e) {
            // Load in config
            AppConfig.Load();
            Exit += OnExit;

            // TODO: We could try to load in files that get passed in as a command line argument

            // Display main window
            MainWindow = new MainWindow();
            MainWindow.Show();
        }

        private void OnExit(object sender, ExitEventArgs e) {
            // Make sure config gets saved when the application exits
            AppConfig.Save();
        }
    }
}
