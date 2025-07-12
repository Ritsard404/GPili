using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;
using ServiceLibrary.Data;
using ServiceLibrary.Utils;

namespace ServiceLibrary.Services
{
    public interface IDatabaseInitializerService
    {
        Task InitializeAsync();
    }

    public class DatabaseInitializerService : IDatabaseInitializerService
    {
        private readonly DataContext _context;
        private readonly ILogger<DatabaseInitializerService> _logger;
        private readonly DataSeedingService _seeder;

        public DatabaseInitializerService(
            DataContext context,
            ILogger<DatabaseInitializerService> logger,
            DataSeedingService seeder)
        {
            _context = context;
            _logger = logger;
            _seeder = seeder;
        }

        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Starting database initialization.");

                // Ensure data folder exists
                var connection = (SqliteConnection)_context.Database.GetDbConnection();
                var folder = Path.GetDirectoryName(connection.DataSource);

                if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                {
                    _logger.LogDebug("Creating database folder: {Folder}", folder);
                    Directory.CreateDirectory(folder);
                }

                // Open  the database connection
                _logger.LogDebug("Opening database connection.");
                await connection.OpenAsync();

                // Apply migrations (will create DB if missing and record history)
                _logger.LogInformation("Applying migrations if any.");
                await _context.Database.MigrateAsync();

                // Optional: seed data
                _logger.LogInformation("Seeding initial data.");
                await _seeder.SeedDataAsync();

                _logger.LogInformation("Database initialization completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database initialization failed.");
                throw;
            }
        }
    }
}
