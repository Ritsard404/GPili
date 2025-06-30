using Microsoft.EntityFrameworkCore;
using ServiceLibrary.Data;
using ServiceLibrary.Utils;

namespace TestData.Extensions
{
    public static class ServicesRegistrations
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Add database context using the exact same path as MAUI app
            services.AddDbContext<DataContext>(options =>
            {
                // Use the exact same database file path as the MAUI app
                var dbPath = Path.Combine(FolderPath.Database.Test, "GPili.db");
                
                // Ensure directory exists
                var dbDirectory = Path.GetDirectoryName(dbPath);
                if (!Directory.Exists(dbDirectory))
                {
                    Directory.CreateDirectory(dbDirectory);
                }

                options.UseSqlite($"Data Source={dbPath}");
            });

            return services;
        }
    }
}
