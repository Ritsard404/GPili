using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceLibrary.Data;
using ServiceLibrary.Services;

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
                var dbPath = Path.Combine("C:\\Users\\Acer\\AppData\\Local\\Packages\\com.companyname.gpili_9zz4h110yvjzm\\LocalState", "GPili.db");
                
                // Ensure directory exists
                var dbDirectory = Path.GetDirectoryName(dbPath);
                if (!Directory.Exists(dbDirectory))
                {
                    Directory.CreateDirectory(dbDirectory);
                }

                options.UseSqlite($"Data Source={dbPath}");
            });

            // Register services with scoped lifetime to match DataContext
            services.AddScoped<IDatabaseInitializerService, DatabaseInitializerService>();
            services.AddScoped<DataSeedingService>();

            return services;
        }
    }
}
