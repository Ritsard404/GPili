using Microsoft.EntityFrameworkCore;
using ServiceLibrary.Data;
using ServiceLibrary.Models;
using ServiceLibrary.Utils;
using System.Diagnostics;

namespace ServiceLibrary.Services
{
    public class DataSeedingService(DataContext _context)
    {

        public async Task SeedDataAsync()
        {
            try
            {
                await SeedUsersAsync();
                await SeedPosTerminalInfoAsync();
                await SeedSaleTypesAsync();

                await SeedCategoriesAndProductsAsync();
            }
            catch (Exception ex)
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
                new() { Email = "200303", FName = "Admin", LName = "Ko", Role = RoleType.Developer, CardId = "0472EE01B14D03" },
                new() { Email = "Honey@stiffy.com", FName = "Honey", LName = "Abella", Role = RoleType.Cashier },
                new() { Email = "Princess@stiffy.com", FName = "Princess", LName = "Abella", Role = RoleType.Cashier },
                new() { Email = "Divine@stiffy.com", FName = "Divine", LName = "Abella", Role = RoleType.Cashier },
                new() { Email = "admin", FName = "Stiffany", LName = "Abella", Role = RoleType.Manager }
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
                ValidUntil = DateTime.Now.AddMonths(5),
                PosName = "1",
                RegisteredName = "GPili Store",
                OperatedBy = "GPili Corporation",
                Address = "123 Main Street, City, Province",
                VatTinNumber = "123-456-789-000",
                CostCenter = "Store 1",
                BranchCenter = "BC001",
                DbName = "arseneso_barandog",
                UseCenter = "MAIN",
                PrinterName = "PB-58H",
                Vat = 0,
                //Vat = 12,
                DiscountMax = 250.00m, // Example VAT max value
            };

            await _context.PosTerminalInfo.AddAsync(posInfo);
        }

        private async Task SeedCategoriesAndProductsAsync()
        {
            if (await _context.Category.AnyAsync()) return;

            var categories = new List<Category>
            {
                new() { CtgryName = "Burgers" },
                new() { CtgryName = "Nachos" },
                new() { CtgryName = "Shawarma" },
                new() { CtgryName = "Fries" },
                new() { CtgryName = "Sizzling/Rice Meals" },
                new() { CtgryName = "Combo's" },
                new() { CtgryName = "Drinks" },
                new() { CtgryName = "Ad-ons" },
                new() { CtgryName = "Sandwich" }
            };

            await _context.Category.AddRangeAsync(categories);

            var products = new List<Product>
            {
                // Burgers
                new() { Name = "Burger Beef Supreme", ProdId = "BURGBSUP", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Piece", Barcode = "480000000001", Price = 50.00m, Category = categories[0] },
                new() { Name = "Burger Aloha", ProdId = "BURGALOH", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Piece", Barcode = "480000000002", Price = 99.00m, Category = categories[0] },
                new() { Name = "Burger Double Patty", ProdId = "BURGDPAT", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Piece", Barcode = "480000000003", Price = 85.00m, Category = categories[0] },

                // Nachos
                new() { Name = "Nachos Small", ProdId = "NACHSM", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Serving", Barcode = "480000000004", Price = 60.00m, Category = categories[1] },
                new() { Name = "Nachos Overload Big", ProdId = "NACHOLB", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Serving", Barcode = "480000000005", Price = 100.00m, Category = categories[1] },
                new() { Name = "Nachos Overload w/ Fries", ProdId = "NACHOLF", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Serving", Barcode = "480000000006", Price = 120.00m, Category = categories[1] },

                // Shawarma
                new() { Name = "Shawarma Beef Wrap", ProdId = "SHAWBW", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Piece", Barcode = "480000000007", Price = 50.00m, Category = categories[2] },

                // Fries
                new() { Name = "Fries Small", ProdId = "FRIESSM", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Serving", Barcode = "480000000008", Price = 30.00m, Category = categories[3] },
                new() { Name = "Fries Big", ProdId = "FRIESBG", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Serving", Barcode = "480000000009", Price = 50.00m, Category = categories[3] },
                new() { Name = "Fries Overload", ProdId = "FRIESOL", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Serving", Barcode = "480000000010", Price = 60.00m, Category = categories[3] },

                // Sizzling / Rice Meals
                new() { Name = "Garlic Pepper Beef", ProdId = "SIZZGPB", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Plate", Barcode = "480000000011", Price = 95.00m, Category = categories[4] },
                new() { Name = "Burger Steak", ProdId = "SIZZBSTEAK", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Plate", Barcode = "480000000012", Price = 70.00m, Category = categories[4] },
                new() { Name = "Sizzling Porkchop", ProdId = "SIZZPCHP", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Plate", Barcode = "480000000013", Price = 99.00m, Category = categories[4] },
                new() { Name = "Sizzling Chicken", ProdId = "SIZZCHK", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Plate", Barcode = "480000000014", Price = 89.00m, Category = categories[4] },
                new() { Name = "Sizzling Hungarian", ProdId = "SIZZHG", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Plate", Barcode = "480000000015", Price = 105.00m, Category = categories[4] },
                new() { Name = "Sizzling Cheeseburger w/ Fries", ProdId = "SIZZCBF", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Plate", Barcode = "480000000016", Price = 125.00m, Category = categories[4] },

                // Combo's
                new() { Name = "Burger w/ Fries", ProdId = "COMBBWF", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Set", Barcode = "480000000017", Price = 80.00m, Category = categories[5] },
                new() { Name = "Shawarma w/ Fries", ProdId = "COMBSWF", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Set", Barcode = "480000000018", Price = 80.00m, Category = categories[5] },
                new() { Name = "Nachos/Fries Overload", ProdId = "COMBNFO", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Set", Barcode = "480000000019", Price = 120.00m, Category = categories[5] },
                new() { Name = "Burger/Nachos", ProdId = "COMBBN", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Set", Barcode = "480000000020", Price = 100.00m, Category = categories[5] },

                // Drinks
                new() { Name = "Coke", ProdId = "DRINKCOKE", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Bottle", Barcode = "480000000021", Price = 15.00m, Category = categories[6] },
                new() { Name = "Mountain Dew", ProdId = "DRINKMD", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Bottle", Barcode = "480000000022", Price = 20.00m, Category = categories[6] },
                new() { Name = "Sprite", ProdId = "DRINKSPR", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Bottle", Barcode = "480000000023", Price = 15.00m, Category = categories[6] },
                new() { Name = "Mineral Water 1L", ProdId = "DRINKMW1L", ItemType = "Resale", VatType = VatType.Exempt, BaseUnit = "Bottle", Barcode = "480000000024", Price = 20.00m, Category = categories[6] },

                // Ad-ons
                new() { Name = "Egg", ProdId = "ADDONEGG", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Piece", Barcode = "480000000025", Price = 15.00m, Category = categories[7] },
                new() { Name = "Cheese", ProdId = "ADDONCHS", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Slice", Barcode = "480000000026", Price = 10.00m, Category = categories[7] },
                new() { Name = "Cheese Sauce", ProdId = "ADDONCHSCS", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Serving", Barcode = "480000000027", Price = 10.00m, Category = categories[7] },
                new() { Name = "Ham", ProdId = "ADDONHAM", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Slice", Barcode = "480000000028", Price = 10.00m, Category = categories[7] },
                new() { Name = "Gravy", ProdId = "ADDONGRV", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Serving", Barcode = "480000000029", Price = 10.00m, Category = categories[7] },
                new() { Name = "Veggies", ProdId = "ADDONVEG", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Serving", Barcode = "480000000030", Price = 10.00m, Category = categories[7] },

                // Sandwich
                new() { Name = "Ham Sandwich w/ Cheese", ProdId = "SANDHMC", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Piece", Barcode = "480000000031", Price = 40.00m, Category = categories[8] },
                new() { Name = "Ham & Egg Sandwich w/ Cheese", ProdId = "SANDHEC", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Piece", Barcode = "480000000032", Price = 45.00m, Category = categories[8] },
                new() { Name = "Egg Sandwich w/ Cheese", ProdId = "SANDEGC", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Piece", Barcode = "480000000033", Price = 40.00m, Category = categories[8] },
                new() { Name = "Club House", ProdId = "SANDCLUB", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Piece", Barcode = "480000000034", Price = 95.00m, Category = categories[8] },
                new() { Name = "Chicken Sandwich", ProdId = "SANDCHK", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Piece", Barcode = "480000000035", Price = 75.00m, Category = categories[8] },
                new() { Name = "Tuna Sandwich", ProdId = "SANDTUNA", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Piece", Barcode = "480000000036", Price = 65.00m, Category = categories[8] }

            };


            //var products = new List<Product>
            //{
            //    new() { Name = "Coca Cola 330ml", ProdId = "COCA330", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Can", Barcode = "4801234567890", Quantity = 50, Price = 25.00m, Category = categories[0] },
            //    new() { Name = "Sprite 330ml", ProdId = "SPRITE330", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Can", Barcode = "4801234567891", Quantity = 45, Price = 25.00m, Category = categories[0] },
            //    new() { Name = "Pepsi 330ml", ProdId = "PEPSI330", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Can", Barcode = "4801234567892", Quantity = 40, Price = 25.00m, Category = categories[0] },
            //    new() { Name = "Potato Chips", ProdId = "POTCHIPS", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Pack", Barcode = "4801234567893", Quantity = 30, Price = 35.00m, Category = categories[1] },
            //    new() { Name = "Cheese Puffs", ProdId = "CHEESEPUFF", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Pack", Barcode = "4801234567894", Quantity = 25, Price = 30.00m, Category = categories[1] },
            //    new() { Name = "Fresh Milk 1L", ProdId = "MILK1L", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Bottle", Barcode = "4801234567895", Quantity = 20, Price = 85.00m, Category = categories[2] },
            //    new() { Name = "Yogurt 500ml", ProdId = "YOGURT500", ItemType = "Resale", VatType = VatType.Vatable, BaseUnit = "Bottle", Barcode = "4801234567896", Quantity = 15, Price = 65.00m, Category = categories[2] },

            //    new() { Name = "Harina", ProdId = "Harina", ItemType = "Wholesale", VatType = VatType.Zero, BaseUnit = "Grams", Barcode = "4801234367896", Quantity = 15, Price = 65.00m, Category = categories[3] },
            //    new() { Name = "Yeast", ProdId = "Yeast", ItemType = "Wholesale", VatType = VatType.Zero, BaseUnit = "Grams", Barcode = "4831234567896", Quantity = 15, Price = 65.00m, Category = categories[3] },
            //    new() { Name = "Asin", ProdId = "Asin", ItemType = "Wholesale", VatType = VatType.Exempt, BaseUnit = "Grams", Barcode = "4801233567896", Quantity = 15, Price = 65.00m, Category = categories[2] },
            //};

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
        };

            await _context.SaleType.AddRangeAsync(saleTypes);
        }
    }
}