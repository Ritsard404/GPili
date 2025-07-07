using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using UraniumUI;

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
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureApplication()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("Nunito-Regular.ttf", "NunitoRegular");
                    fonts.AddFont("Nunito-Semibold.ttf", "NunitoSemibold");
                    fonts.AddFont("Nunito-Bold.ttf", "NunitoBold");
                    fonts.AddFont("Nunito-ExtraBold.ttf", "NunitoExtrabold");
                    fonts.AddFont("Nunito-Black.ttf", "NunitoBlack");
                    fonts.AddFontAwesomeIconFonts();
                })
                .UseMauiCommunityToolkit()
                .UseUraniumUI()
                .UseUraniumUIMaterial();


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
            builder.Logging.AddDebug();
#endif

            // Build the app first
            var app = builder.Build();

            // Now safely resolve the database initializer
            using (var scope = app.Services.CreateScope())
            {
                var dbInitializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializerService>();
                dbInitializer.InitializeAsync().GetAwaiter().GetResult();
            }

            return app;
        }
    }
}
