using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceLibrary.Data;
using System.Diagnostics;

namespace ServiceLibrary.Services
{
    public interface IDatabaseInitializerService
    {
        Task InitializeDatabaseAsync();
    }

    public class DatabaseInitializerService(DataContext _context) : IDatabaseInitializerService
    {

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                Debug.WriteLine("Starting database initialization...");

                var connectionString = _context.Database.GetConnectionString();
                Debug.WriteLine($"Connection string: {connectionString}");

                var dbFilePath = connectionString?.Split('=')[1]; // crude but works for SQLite "Data Source=GPili.db"
                Debug.WriteLine($"Database file path: {dbFilePath}");

                var dbFolder = Path.GetDirectoryName(dbFilePath);
                Debug.WriteLine($"Database folder: {dbFolder}");

                if (!string.IsNullOrEmpty(dbFolder) && !Directory.Exists(dbFolder))
                {
                    Debug.WriteLine($"Creating directory: {dbFolder}");
                    Directory.CreateDirectory(dbFolder);
                }

                // Always ensure database and tables are created
                Debug.WriteLine("Ensuring database and tables are created...");
                await _context.Database.EnsureCreatedAsync();
                Debug.WriteLine("Database and tables created successfully");

                // Verify database connection
                if (await _context.Database.CanConnectAsync())
                {
                    Debug.WriteLine("Successfully connected to database");
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
