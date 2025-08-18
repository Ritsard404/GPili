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

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
