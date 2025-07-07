

using CommunityToolkit.Maui;
using GPili.Presentation.Contents.Cashiering;
using GPili.Presentation.Features.Cashiering;
using GPili.Presentation.Features.LogIn;
using GPili.Presentation.Features.Manager;
using GPili.Presentation.Popups;
using GPili.Presentation.Popups.Manager;
using ServiceLibrary.Extension;
using ServiceLibrary.Utils;

namespace GPili.Extensions;

internal static class ApplicationExtensions
{
    public static MauiAppBuilder ConfigureApplication(this MauiAppBuilder builder)
    {
        builder.Services
            .AddDatabase()
            .AddApplicationServices()
            .AddService()
            .RegisterViews()
            .RegisterPopups();

        return builder;
    }
    public static IServiceCollection AddDatabase(this IServiceCollection services)
    {
        string dbPath;

        #if DEBUG
                // Use test path in Debug mode
                dbPath = Path.Combine(FolderPath.Database.Test, "GPili.db");
        #else
            // Use persistent path in Release mode
            dbPath = GetPersistentDatabasePath();
        #endif

        // Ensure directory exists
        var dbDirectory = Path.GetDirectoryName(dbPath);
        if (!Directory.Exists(dbDirectory))
            Directory.CreateDirectory(dbDirectory);

        var connectionString = $"Data Source={dbPath}";

        services.AddDbContext<DataContext>(options =>
            options.UseSqlite(connectionString, x => x.MigrationsAssembly(nameof(ServiceLibrary))));

        return services;
    }
    private static string GetPersistentDatabasePath()
    {
        #if ANDROID
            // External public path (survives uninstall with permission)
            var basePath = Path.Combine("/storage/emulated/0/YourAppName/Database");
        #elif WINDOWS || MACCATALYST
                // App-scoped local data
                var basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GPili");
        #else
            // Default MAUI internal storage
            var basePath = Path.Combine(FileSystem.AppDataDirectory, "Database");
        #endif

        return Path.Combine(basePath, "GPili.db");
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IPopUpService, PopUpService>();

        return services;
    }

    public static IServiceCollection RegisterViews(this IServiceCollection services)
    {
        // Register your views here
        services.AddSingleton<AppShell>();

        services.AddPageViewModel<LogInViewModel, LogInPage>();
        services.AddPageViewModel<CashieringViewModel, CashieringPage>();
        services.AddPageViewModel<ManagerViewModel, ManagerPage>();

        return services;
    }

    public static IServiceCollection RegisterPopups(this IServiceCollection services)
    {
        // Register your popups here
        services.AddTransientPopup<LoaderView, LoaderViewModel>();
        services.AddTransientPopup<ManagerAuthView, ManagerAuthViewModel>();
        services.AddTransientPopup<EditItemView, EditItemViewModel>();
        services.AddTransientPopup<EPaymentView, EPaymentViewModel>();
        services.AddTransientPopup<DiscountView, DiscountViewModel>();
        services.AddTransientPopup<DateSelectionPopup, SelectionOfDateViewModel>();

        return services;
    }

    private static void AddPageViewModel<TViewModel, TView>(this IServiceCollection services)
        where TView : ContentPage, new()
        where TViewModel : class
    {
        services.AddTransient<TViewModel>();
        services.AddTransient<TView>(s => new TView() { BindingContext = s.GetRequiredService<TViewModel>() });
    }
}