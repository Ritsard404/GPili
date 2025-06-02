using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServiceLibrary.Data;
using ServiceLibrary.Services;

namespace GPili.Extensions
{
    internal static class ApplicationRegistrations
    {
        public static MauiAppBuilder RegisterServices(this MauiAppBuilder builder)
        {
            // Register logging
            builder.Logging.AddDebug();

            // Register DataContext
            builder.Services.AddDbContext<DataContext>(options =>
            {
                var dbPath = Path.Combine(FileSystem.AppDataDirectory, "GPili.db");
                options.UseSqlite($"Data Source={dbPath}");
            });

            // Register database initializer with logging
            builder.Services.AddSingleton<IDatabaseInitializerService, DatabaseInitializerService>();
            
            return builder;
        }
    }
}
