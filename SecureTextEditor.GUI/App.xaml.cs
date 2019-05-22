using System.Windows;
using SecureTextEditor.GUI.Config;

namespace SecureTextEditor.GUI {
    /// <summary>
    /// Startup logic for the actual Application.
    /// </summary>
    public partial class App : Application {
        protected override void OnStartup(StartupEventArgs e) {
            // Load in config
            AppConfig.Load();

            // If we get passed in an argument treat it as a path for a file to initially load
            string path = null;
            if (e.Args.Length > 0) {
                path = e.Args[0];
            }

            // Display main window
            MainWindow window = new MainWindow();
            MainWindow = window;
            window.Show();

            // If we got a path try opening it now
            if (path != null) {
                window.OpenFile(path);
            }
        }

        protected override void OnExit(ExitEventArgs e) {
            // Make sure config gets saved when the application exits
            AppConfig.Save();

            base.OnExit(e);
        }
    }
}
