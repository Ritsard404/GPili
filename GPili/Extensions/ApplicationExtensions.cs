using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using ServiceLibrary.Data;
using ServiceLibrary.Services;

namespace GPili.Extensions;

internal static class ApplicationExtensions
{
    public static MauiAppBuilder ConfigureApplication(this MauiAppBuilder builder)
    {
        builder.Services
            .AddApplicationServices()
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
        services.AddScoped<IDatabaseInitializerService, DatabaseInitializerService>();
        services.AddScoped<DataSeedingService>();

        // Add your other services here following the pattern:
        // services.AddScoped<IYourService, YourService>();
        // services.AddSingleton<IYourSingletonService, YourSingletonService>();
        // services.AddTransient<IYourTransientService, YourTransientService>();

        return services;
    }
} 