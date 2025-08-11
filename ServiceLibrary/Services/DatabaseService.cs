using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;
using ServiceLibrary.Data;
using ServiceLibrary.Utils;

namespace ServiceLibrary.Services
{
    public interface IDatabaseService
    {
        Task InitializeAsync();
        Task ResetDatabaseAsync();
    }

    public class DatabaseService(DataContext _context,
        ILogger<DatabaseService> _logger,
        DataSeedingService _seeder) : IDatabaseService
    {

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

        public async Task ResetDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("Starting database reset process.");
                
                // First backup the database
                await BackupDatabaseAsync();
                
                // Then reset the database
                await ResetDatabaseDataAsync();
                
                _logger.LogInformation("Database reset process completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database reset process failed.");
                throw;
            }
        }

        private async Task BackupDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("Starting database backup.");

                var connection = (SqliteConnection)_context.Database.GetDbConnection();
                var sourceDbPath = connection.DataSource;

                if (!File.Exists(sourceDbPath))
                {
                    _logger.LogWarning("Database file does not exist, skipping backup.");
                    return;
                }

                // Create backup filename with timestamp
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"GPili_Backup_{timestamp}.db";
                var backupPath = Path.Combine(FolderPath.Database.BackUp, backupFileName);

                // Ensure backup directory exists
                var backupDirectory = Path.GetDirectoryName(backupPath);
                if (!Directory.Exists(backupDirectory))
                {
                    Directory.CreateDirectory(backupDirectory);
                }

                // Close the connection before copying
                await connection.CloseAsync();

                // Copy the database file
                File.Copy(sourceDbPath, backupPath, true);

                // Reopen the connection
                await connection.OpenAsync();

                _logger.LogInformation("Database backed up successfully to: {BackupPath}", backupPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database backup failed.");
                throw;
            }
        }

        private async Task ResetDatabaseDataAsync()
        {
            try
            {
                _logger.LogInformation("Starting database data truncation.");

                // Clear any existing tracked entities to prevent conflicts
                _context.ChangeTracker.Clear();

                // Disable foreign key constraints temporarily
                await _context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=OFF");

                // Get all table names except the __EFMigrationsHistory table
                var tableNames = await _context.Database
                    .SqlQueryRaw<string>("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name != '__EFMigrationsHistory'")
                    .ToListAsync();

                foreach (var tableName in tableNames)
                {
                    // Skip tables to preserve their data
                    var tablesToPreserve = new[] { "User", "SaleType", "Product", "Category", "PosTerminalInfo" };
                    if (tablesToPreserve.Any(t => tableName.Equals(t, StringComparison.OrdinalIgnoreCase)))
                    {
                        _logger.LogInformation("Skipping table to preserve data: {TableName}", tableName);
                        continue;
                    }

                    try
                    {
                        // Delete all data from the table
                        await _context.Database.ExecuteSqlRawAsync($"DELETE FROM \"{tableName}\"");
                        _logger.LogInformation("Truncated table: {TableName}", tableName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Failed to truncate table {TableName}: {Message}", tableName, ex.Message);
                    }
                }

                // Re-enable foreign key constraints
                await _context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=ON");

                // Reset auto-increment counters for truncated tables only
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM sqlite_sequence WHERE name IN ('AuditLog', 'Invoice', 'Item', 'Timestamp', 'Journal', 'EPayment', 'Inventory', 'Reading', 'InvoiceDocument')");

                // Don't reseed data since preserved tables already contain the necessary data
                // This prevents entity tracking conflicts
                _logger.LogInformation("Skipping data seeding to avoid conflicts with preserved data.");

                _logger.LogInformation("Database data truncation completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database data truncation failed.");
                throw;
            }
        }
    }
}
