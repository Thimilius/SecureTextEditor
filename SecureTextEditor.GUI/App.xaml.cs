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

            // If we get passed in an argument treat it as a path for a file to initially load
            string path = null;
            if (e.Args.Length > 0) {
                path = e.Args[0];
            }

            // Display main window
            MainWindow = new MainWindow(path);
            MainWindow.Show();
        }

        private void OnExit(object sender, ExitEventArgs e) {
            // Make sure config gets saved when the application exits
            AppConfig.Save();
        }
    }
}
