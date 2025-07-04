using Microsoft.EntityFrameworkCore;
using ServiceLibrary.Data;
using ServiceLibrary.Models;
using ServiceLibrary.Utils;
using System.Diagnostics;

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
            try
            {
                await SeedUsersAsync();
                await SeedPosTerminalInfoAsync();
                //await SeedCategoriesAndProductsAsync();
                await SeedSaleTypesAsync();
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                await _context.SaveChangesAsync();
            }
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
                PosName = "1",
                RegisteredName = "GPili Store",
                OperatedBy = "GPili Corporation",
                Address = "123 Main Street, City, Province",
                VatTinNumber = "123-456-789-000",
                CostCenter = "Store 1",
                BranchCenter = "BC001",
                DbName = "arseneso_barandog",
                UseCenter = "MAIN",
                PrinterName = "POSPrinter",
                Vat = 12,
                DiscountMax = 250.00m, // Example VAT max value
            };

            await _context.PosTerminalInfo.AddAsync(posInfo);
        }

        private async Task SeedCategoriesAndProductsAsync()
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

            var products = new List<Product>
            {
                new() { Name = "Coca Cola 330ml", ProdId = "COCA330", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Can", Barcode = "4801234567890", Quantity = 50, Price = 25.00m, Category = categories[0] },
                new() { Name = "Sprite 330ml", ProdId = "SPRITE330", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Can", Barcode = "4801234567891", Quantity = 45, Price = 25.00m, Category = categories[0] },
                new() { Name = "Pepsi 330ml", ProdId = "PEPSI330", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Can", Barcode = "4801234567892", Quantity = 40, Price = 25.00m, Category = categories[0] },
                new() { Name = "Potato Chips", ProdId = "POTCHIPS", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Pack", Barcode = "4801234567893", Quantity = 30, Price = 35.00m, Category = categories[1] },
                new() { Name = "Cheese Puffs", ProdId = "CHEESEPUFF", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Pack", Barcode = "4801234567894", Quantity = 25, Price = 30.00m, Category = categories[1] },
                new() { Name = "Fresh Milk 1L", ProdId = "MILK1L", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Bottle", Barcode = "4801234567895", Quantity = 20, Price = 85.00m, Category = categories[2] },
                new() { Name = "Yogurt 500ml", ProdId = "YOGURT500", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Bottle", Barcode = "4801234567896", Quantity = 15, Price = 65.00m, Category = categories[2] },

                new() { Name = "Harina", ProdId = "Harina", ItemType = "Wholesale", VatType = VatType.Zero, BaseUnit = "Grams", Barcode = "4801234367896", Quantity = 15, Price = 65.00m, Category = categories[3] },
                new() { Name = "Yeast", ProdId = "Yeast", ItemType = "Wholesale", VatType = VatType.Zero, BaseUnit = "Grams", Barcode = "4831234567896", Quantity = 15, Price = 65.00m, Category = categories[3] },
                new() { Name = "Asin", ProdId = "Asin", ItemType = "Wholesale", VatType = VatType.Exempt, BaseUnit = "Grams", Barcode = "4801233567896", Quantity = 15, Price = 65.00m, Category = categories[2] },
            };

            await _context.Product.AddRangeAsync(products);
        }
        private async Task SeedSaleTypesAsync()
        {
            if (await _context.SaleType.AnyAsync()) return;

            var saleTypes = new List<SaleType>
        {
            new() { Name = "GCash", Account = "GCASH-001", Type = "E-Payment"},
            new() { Name = "PayMaya", Account = "PAYMAYA-001", Type = "E-Payment"},
            new() { Name = "Credit Card", Account = "CC-001", Type = "Card"},
            new() { Name = "Debit Card", Account = "DC-001", Type = "Card"},
            new() { Name = "Gift Certificate", Account = "GC-001", Type = "Voucher"}
        };

            await _context.SaleType.AddRangeAsync(saleTypes);
        }
    }
} 