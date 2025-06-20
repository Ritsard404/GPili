

using CommunityToolkit.Maui;
using GPili.Presentation.Features.Cashiering;
using GPili.Presentation.Features.LogIn;
using GPili.Presentation.Features.Manager;
using GPili.Presentation.Popups;
using ServiceLibrary.Extension;

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
        //var dbPath = Path.Combine(FileSystem.AppDataDirectory, "GPili.db");
        var dbPath = Path.Combine("C:\\Users\\Acer\\Documents", "GPili.db");
        var connectionString = $"Data Source={dbPath}";

        services.AddDbContext<DataContext>(options =>
            options.UseSqlite(connectionString, x => x.MigrationsAssembly(nameof(ServiceLibrary))));

        return services;
    }
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Database Services - using scoped to match DataContext lifetime

        // Add your other services here following the pattern:
        // services.AddScoped<IYourService, YourService>();
        // services.AddSingleton<IYourSingletonService, YourSingletonService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ILoaderService, LoaderService>();

        return services;
    }

    public static IServiceCollection RegisterViews(this IServiceCollection services)
    {
        // Register your views here
        services.AddSingleton<AppShell>();

        services.AddViewModel<LogInViewModel, LogInPage>();
        services.AddViewModel<CashieringViewModel, CashieringPage>();
        services.AddViewModel<ManagerViewModel, ManagerPage>();
        return services;
    }

    public static IServiceCollection RegisterPopups(this IServiceCollection services)
    {
        // Register your popups here
        services.AddTransientPopup<LoaderView, LoaderViewModel>();
        //services.AddPopUpViewModel<LoaderView, LoaderViewModel>();

        return services;
    }

    private static void AddViewModel<TViewModel, TView>(this IServiceCollection services)
        where TView : ContentPage, new()
        where TViewModel : class
    {
        services.AddTransient<TViewModel>();
        services.AddTransient<TView>(s => new TView() { BindingContext = s.GetRequiredService<TViewModel>() });
    }

    private static void AddPopUpViewModel<TView, TViewModel>(this IServiceCollection services)
        where TView : Popup, new()
        where TViewModel : class
    {
        services.AddTransient<TViewModel>();
        services.AddTransient<TView>(s => new TView() { BindingContext = s.GetRequiredService<TViewModel>() });
    }
}