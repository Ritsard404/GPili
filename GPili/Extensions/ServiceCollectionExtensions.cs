using Microsoft.Extensions.DependencyInjection;
using ServiceLibrary.Services;
using ServiceLibrary.Services.Interfaces;

namespace GPili.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Database Services
        services.AddSingleton<IDatabaseInitializerService, DatabaseInitializerService>();
        
        // Add your other services here following the pattern:
        // services.AddScoped<IYourService, YourService>();
        // services.AddSingleton<IYourSingletonService, YourSingletonService>();
        // services.AddTransient<IYourTransientService, YourTransientService>();

        return services;
    }
} 