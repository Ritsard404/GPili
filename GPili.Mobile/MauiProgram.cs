using CommunityToolkit.Maui;
using GPili.Extensions;
using Microsoft.Extensions.Logging;
using UraniumUI;

namespace GPili.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            try
            {

                SQLitePCL.Batteries_V2.Init();

                var builder = MauiApp.CreateBuilder();
                builder
                    .UseMauiApp<App>()
                    .ConfigureApplication()
                    .UseMauiCommunityToolkit()
                    .ConfigureFonts(fonts =>
                    {
                        fonts.AddFont("Nunito-Regular.ttf", "NunitoRegular");
                        fonts.AddFont("Nunito-Semibold.ttf", "NunitoSemibold");
                        fonts.AddFont("Nunito-Bold.ttf", "NunitoBold");
                        fonts.AddFont("Nunito-ExtraBold.ttf", "NunitoExtrabold");
                        fonts.AddFont("Nunito-Black.ttf", "NunitoBlack");
                        fonts.AddFontAwesomeIconFonts();
                    })
                    .UseUraniumUI()
                    .UseUraniumUIMaterial();
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    LogToFile($"[FATAL] UnhandledException: {e.ExceptionObject}");
                };

                TaskScheduler.UnobservedTaskException += (s, e) =>
                {
                    LogToFile($"[FATAL] UnobservedTaskException: {e.Exception}");
                    e.SetObserved();
                };

#if DEBUG
                builder.Logging.AddDebug();
#endif

                return builder.Build();

            }
            catch (Exception ex)
            {
                LogToFile($"Startup Error: {ex}");
                throw; // Optional: rethrow if you want app to crash after logging
            }
        }

        private static void LogToFile(string message)
        {
            try
            {
#if ANDROID
                // Store in Android's public Documents folder
                var logDir = Android.OS.Environment
                    .GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDocuments)
                    .AbsolutePath;
#else
                // Cross-platform app data storage
                var logDir = FileSystem.AppDataDirectory;
#endif
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);

                var logFile = Path.Combine(logDir, "maui-startup.log");
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                File.AppendAllText(logFile, $"[{timestamp}] {message}{Environment.NewLine}");
            }
            catch
            {
                // Avoid recursive logging failure
            }
        }
    }
}
