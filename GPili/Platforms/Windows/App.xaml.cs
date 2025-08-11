using Microsoft.UI.Xaml;
using System;
using System.IO;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GPili.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            try
            {
                this.InitializeComponent();
                LogToFile("App constructor completed successfully");
            }
            catch (Exception ex)
            {
                LogToFile($"App constructor error: {ex}");
                throw; // Optionally rethrow to crash the app
            }
        }

        protected override MauiApp CreateMauiApp()
        {
            try
            {
                LogToFile("CreateMauiApp started");
                var app = MauiProgram.CreateMauiApp();
                LogToFile("CreateMauiApp completed successfully");
                return app;
            }
            catch (Exception ex)
            {
                LogToFile($"CreateMauiApp error: {ex}");
                throw;
            }
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                LogToFile("OnLaunched started");
                base.OnLaunched(args);
                LogToFile("OnLaunched completed successfully");
            }
            catch (Exception ex)
            {
                LogToFile($"OnLaunched error: {ex}");
                throw;
            }
        }

        private static void LogToFile(string message)
        {
            try
            {
                var logDir = @"C:\GPili";
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);
                
                var logFile = Path.Combine(logDir, "app-startup.log");
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                File.AppendAllText(logFile, $"[{timestamp}] {message}{Environment.NewLine}");
            }
            catch { /* Ignore logging errors */ }
        }
    }
}
