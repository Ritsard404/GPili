using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceLibrary.Data;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ServiceLibrary.Services
{
    public interface IDatabaseInitializerService
    {
        Task InitializeDatabaseAsync();
        Task<bool> HasPendingMigrationsAsync();
    }

    public class DatabaseInitializerService : IDatabaseInitializerService
    {
        private readonly DataContext _context;
        private readonly ILogger<DatabaseInitializerService> _logger;

        public DatabaseInitializerService(DataContext context, ILogger<DatabaseInitializerService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> HasPendingMigrationsAsync()
        {
            try
            {
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
                return pendingMigrations.Any();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking pending migrations: {ex.Message}");
                return false;
            }
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                Debug.WriteLine("Starting database initialization...");

                var connectionString = _context.Database.GetConnectionString();
                Debug.WriteLine($"Connection string: {connectionString}");

                var dbFilePath = connectionString?.Split('=')[1];
                Debug.WriteLine($"Database file path: {dbFilePath}");

                var dbFolder = Path.GetDirectoryName(dbFilePath);
                Debug.WriteLine($"Database folder: {dbFolder}");

                if (!string.IsNullOrEmpty(dbFolder) && !Directory.Exists(dbFolder))
                {
                    Debug.WriteLine($"Creating directory: {dbFolder}");
                    Directory.CreateDirectory(dbFolder);
                }

                // Check if database exists
                if (!await _context.Database.CanConnectAsync())
                {
                    Debug.WriteLine("Database does not exist. Creating...");
                    await _context.Database.EnsureCreatedAsync();
                    Debug.WriteLine("Database created successfully");
                }

                // Check for pending migrations
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    Debug.WriteLine($"Found {pendingMigrations.Count()} pending migrations:");
                    foreach (var migration in pendingMigrations)
                    {
                        Debug.WriteLine($"- {migration}");
                    }
                    Debug.WriteLine("Applying pending migrations...");
                    await _context.Database.MigrateAsync();
                    Debug.WriteLine("Migrations applied successfully");
                }
                else
                {
                    Debug.WriteLine("No pending migrations found");
                }

                // Verify database connection and tables
                if (await _context.Database.CanConnectAsync())
                {
                    var tables = await _context.Database.SqlQueryRaw<string>(
                        "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name NOT LIKE '__EF%'")
                        .ToListAsync();

                    Debug.WriteLine($"Successfully connected to database. Found {tables.Count} tables:");
                    foreach (var table in tables)
                    {
                        Debug.WriteLine($"- {table}");
                    }
                }
                else
                {
                    Debug.WriteLine("WARNING: Cannot connect to database after initialization");
                }

                Debug.WriteLine("Database initialization completed successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR in database initialization: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}
