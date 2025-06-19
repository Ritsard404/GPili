using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

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
                })
                .UseMauiCommunityToolkit();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
