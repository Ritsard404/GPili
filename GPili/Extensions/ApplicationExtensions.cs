

using GPili.Presentation.Features.LogIn;
using GPili.Presentation.Features.Manager;
using ServiceLibrary.Extension;

namespace GPili.Extensions;

internal static class ApplicationExtensions
{
    public static MauiAppBuilder ConfigureApplication(this MauiAppBuilder builder)
    {
        builder.Services
            .AddApplicationServices()
            .AddService()
            .RegisterViews()
            .AddDatabase();

        // Add any other application-level configurations here
        // For example:
        // - Configure AutoMapper
        // - Configure HttpClient
        // - Configure Authentication
        // - Configure App Settings

        return builder;
    }
    public static IServiceCollection AddDatabase(this IServiceCollection services)
    {
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "GPili.db");
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

        return services;
    }

    public static IServiceCollection RegisterViews(this IServiceCollection services)
    {
        // Register your views here
         services.AddSingleton<AppShell>();
         services.AddTransient<MainPage>();
         services.AddTransient<LogInPage>();
         services.AddTransient<ManagerPage>();
        return services;
    }
} 