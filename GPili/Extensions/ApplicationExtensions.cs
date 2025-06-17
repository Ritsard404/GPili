using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;

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

    public static async Task InitializeApplicationAsync(this IServiceProvider serviceProvider)
    {
        // Initialize database
        await serviceProvider.EnsureDatabaseInitializedAsync();

        // Add any other initialization tasks here
        // For example:
        // - Load application settings
        // - Initialize cache
        // - Warm up services
    }
} 