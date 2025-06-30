using Microsoft.EntityFrameworkCore;
using ServiceLibrary.Models;

namespace ServiceLibrary.Data
{
    public class DataContext : DbContext
    {
        public DataContext()
        {
            
        }

        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
            // Remove EnsureCreated as we'll use migrations instead
            Database.Migrate();
        }

        public DbSet<Product> Product { get; set; }
        public DbSet<Inventory> Inventory { get; set; }
        public DbSet<AlternativePayment> AlternativePayment { get; set; }
        public DbSet<AuditLog> AuditLog { get; set; }
        public DbSet<Category> Category { get; set; }
        public DbSet<Invoice> Invoice { get; set; }
        public DbSet<Item> Item { get; set; }
        public DbSet<PosTerminalInfo> PosTerminalInfo { get; set; }
        public DbSet<SaleType> SaleType { get; set; }
        public DbSet<Timestamp> Timestamp { get; set; }
        public DbSet<User> User { get; set; }
        public DbSet<Journal> AccountJournal { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlite();
    }
}
