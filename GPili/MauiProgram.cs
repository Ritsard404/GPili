using CommunityToolkit.Maui;
using InputKit.Handlers;
using Microsoft.Extensions.Logging;
using UraniumUI;
using Microsoft.Data.Sqlite;
using System;
using System.IO;


#if WINDOWS
using Microsoft.Maui.LifecycleEvents;
using Microsoft.UI;
using Microsoft.UI.Windowing;
#endif

namespace GPili
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            try
            {
                LogToFile("MauiProgram.CreateMauiApp started");

#if WINDOWS
                LogToFile("Initializing SQLite for Windows");
                SQLitePCL.Batteries_V2.Init();
                LogToFile("SQLite initialization completed");
#endif

                LogToFile("Creating MauiApp builder");
                var builder = MauiApp.CreateBuilder();
                
                LogToFile("Configuring MAUI app");
                builder
                    .UseMauiApp<App>()
                    .ConfigureMauiHandlers(handlers =>
                    {
                        LogToFile("Adding InputKit handlers");
                        handlers.AddInputKitHandlers();
                    })
                    .UseMauiCommunityToolkit(option =>
                    {
                        LogToFile("Configuring Community Toolkit");
                        option.SetShouldEnableSnackbarOnWindows(true);
                    })
                    .ConfigureApplication()
                    .ConfigureFonts(fonts =>
                    {
                        LogToFile("Configuring fonts");
                        fonts.AddFont("Nunito-Regular.ttf", "NunitoRegular");
                        fonts.AddFont("Nunito-Semibold.ttf", "NunitoSemibold");
                        fonts.AddFont("Nunito-Bold.ttf", "NunitoBold");
                        fonts.AddFont("Nunito-ExtraBold.ttf", "NunitoExtrabold");
                        fonts.AddFont("Nunito-Black.ttf", "NunitoBlack");
                        fonts.AddFontAwesomeIconFonts();
                    })
                    .UseUraniumUI()
                    .UseUraniumUIMaterial();

                LogToFile("MAUI configuration completed");


#if WINDOWS
                    //maximized window on startup in Windows platform
                    builder.ConfigureLifecycleEvents(events =>
                    {
                        events.AddWindows(wndLifeCycleBuilder =>
                        {
                            wndLifeCycleBuilder.OnWindowCreated(window =>
                            {
                                IntPtr nativeWindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                                Microsoft.UI.WindowId win32WindowsId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(nativeWindowHandle);
                                Microsoft.UI.Windowing.AppWindow winuiAppWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(win32WindowsId);
                                winuiAppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);
                                if (winuiAppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter p)
                                {
                                    //maximize window
                                    p.Maximize();
                                    //disable resizing
                                    p.IsResizable = false;
                                    p.IsMaximizable = false;
                                    p.IsMinimizable = false;
                                }

                                winuiAppWindow.Closing += (s, e) =>
                                {
                                    e.Cancel = true; // prevent closing
                                };
                            });
                        });
                    });
#endif

#if DEBUG
                LogToFile("Adding debug logging");
                builder.Logging.AddDebug();
#endif

                LogToFile("Building MauiApp");
                // Build the app first
                var app = builder.Build();
                LogToFile("MauiApp built successfully");

                // Now safely resolve the database initializer
                LogToFile("Initializing database");
                using (var scope = app.Services.CreateScope())
                {
                    var dbInitializer = scope.ServiceProvider.GetRequiredService<IDatabaseService>();
                    dbInitializer.InitializeAsync().GetAwaiter().GetResult();
                }
                LogToFile("Database initialization completed");

                LogToFile("MauiProgram.CreateMauiApp completed successfully");
                return app;
            }
            catch (Exception ex)
            {
                LogToFile($"MauiProgram.CreateMauiApp error: {ex}");
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
                
                var logFile = Path.Combine(logDir, "maui-startup.log");
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                File.AppendAllText(logFile, $"[{timestamp}] {message}{Environment.NewLine}");
            }
            catch { /* Ignore logging errors */ }
        }

    }
}
