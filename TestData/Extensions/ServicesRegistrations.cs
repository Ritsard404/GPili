using Microsoft.EntityFrameworkCore;
using ServiceLibrary.Data;
using ServiceLibrary.Services;

namespace TestData.Extensions
{
    public static class ServicesRegistrations
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Add database context
            services.AddDbContext<DataContext>(options =>
            {
                // Use the same database file as the MAUI app
                var dbPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "GPili",
                    "GPili.db");
                
                // Ensure directory exists
                var dbDirectory = Path.GetDirectoryName(dbPath);
                if (!Directory.Exists(dbDirectory))
                {
                    Directory.CreateDirectory(dbDirectory);
                }

                options.UseSqlite($"Data Source={dbPath}");
            });

            // Register services
            services.AddScoped<IDatabaseInitializerService, DatabaseInitializerService>();

            return services;
        }
    }
}
