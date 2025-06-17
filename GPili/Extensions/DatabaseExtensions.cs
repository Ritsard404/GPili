using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLibrary.Data;
using ServiceLibrary.Services;

namespace GPili.Extensions;

internal static class DatabaseExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services)
    {
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "GPili.db");
        var connectionString = $"Data Source={dbPath}";
        
        services.AddDbContext<DataContext>(options => 
            options.UseSqlite(connectionString, 
                sqliteOptions => sqliteOptions.MigrationsAssembly("GPili.Migrations")));

        return services;
    }

    public static async Task EnsureDatabaseInitializedAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializerService>();

        // Ensure database is created
        await dbContext.Database.EnsureCreatedAsync();

        // Run migrations if any
        if (dbContext.Database.GetPendingMigrations().Any())
        {
            await dbContext.Database.MigrateAsync();
        }

        // Initialize database with seed data
        await initializer.InitializeDatabaseAsync();
    }
} 