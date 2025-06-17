using Microsoft.EntityFrameworkCore;
using ServiceLibrary.Data;
using ServiceLibrary.Models;
using ServiceLibrary.Utils;

namespace ServiceLibrary.Services
{
    public class DataSeedingService
    {
        private readonly DataContext _context;

        public DataSeedingService(DataContext context)
        {
            _context = context;
        }

        public async Task SeedDataAsync()
        {
            await SeedUsersAsync();
            await SeedPosTerminalInfoAsync();
            await SeedCategoriesAsync();
            await SeedProductsAsync();
            await _context.SaveChangesAsync();
        }

        private async Task SeedUsersAsync()
        {
            if (await _context.User.AnyAsync()) return;

            var users = new List<User>
            {
                new() { Email = "ebisx@gpili.com", FName = "Admin", LName = "User", Role = RoleType.Developer },
                new() { Email = "cashier@gpili.com", FName = "John", LName = "Cashier", Role = RoleType.Cashier },
                new() { Email = "manager@gpili.com", FName = "Jane", LName = "Manager", Role = RoleType.Manager }
            };

            await _context.User.AddRangeAsync(users);
        }

        private async Task SeedPosTerminalInfoAsync()
        {
            if (await _context.PosTerminalInfo.AnyAsync()) return;

            var posInfo = new PosTerminalInfo
            {
                PosSerialNumber = "POS-2024-001",
                MinNumber = "MIN123456789",
                AccreditationNumber = "ACC987654321",
                PtuNumber = "PTU456789123",
                DateIssued = DateTime.Now,
                ValidUntil = DateTime.Now.AddYears(5),
                RegisteredName = "GPili Store",
                OperatedBy = "GPili Corporation",
                Address = "123 Main Street, City, Province",
                VatTinNumber = "123-456-789-000",
                CostCenter = "CC001",
                BranchCenter = "BC001"
            };

            await _context.PosTerminalInfo.AddAsync(posInfo);
        }

        private async Task SeedCategoriesAsync()
        {
            if (await _context.Category.AnyAsync()) return;

            var categories = new List<Category>
            {
                new() { CtgryName = "Beverages" },
                new() { CtgryName = "Snacks" },
                new() { CtgryName = "Dairy" },
                new() { CtgryName = "Bread & Pastries" },
                new() { CtgryName = "Fruits & Vegetables" },
                new() { CtgryName = "Household" },
                new() { CtgryName = "Personal Care" }
            };

            await _context.Category.AddRangeAsync(categories);
        }

        private async Task SeedProductsAsync()
        {
            if (await _context.Product.AnyAsync()) return;

            var categories = await _context.Category.ToListAsync();
            var beverages = categories.FirstOrDefault(c => c.CtgryName == "Beverages");
            var snacks = categories.FirstOrDefault(c => c.CtgryName == "Snacks");
            var dairy = categories.FirstOrDefault(c => c.CtgryName == "Dairy");

            if (beverages == null || snacks == null || dairy == null) return;

            var products = new List<Product>
            {
                new() { Name = "Coca Cola 330ml", Barcode = "4801234567890", Quantity = 50, Price = 25.00m, Category = beverages },
                new() { Name = "Sprite 330ml", Barcode = "4801234567891", Quantity = 45, Price = 25.00m, Category = beverages },
                new() { Name = "Pepsi 330ml", Barcode = "4801234567892", Quantity = 40, Price = 25.00m, Category = beverages },
                new() { Name = "Potato Chips", Barcode = "4801234567893", Quantity = 30, Price = 35.00m, Category = snacks },
                new() { Name = "Cheese Puffs", Barcode = "4801234567894", Quantity = 25, Price = 30.00m, Category = snacks },
                new() { Name = "Fresh Milk 1L", Barcode = "4801234567895", Quantity = 20, Price = 85.00m, Category = dairy },
                new() { Name = "Yogurt 500ml", Barcode = "4801234567896", Quantity = 15, Price = 65.00m, Category = dairy }
            };

            await _context.Product.AddRangeAsync(products);
        }
    }
} 