using Microsoft.EntityFrameworkCore;
using ServiceLibrary.Data;
using ServiceLibrary.Models;
using ServiceLibrary.Services.DTO.Inventory;
using ServiceLibrary.Services.Interfaces;
using ServiceLibrary.Services.PDF;
using ServiceLibrary.Utils;
using System.Diagnostics;
using System.Net.Http.Json;
using static ServiceLibrary.Utils.FolderPath;

namespace ServiceLibrary.Services.Repositories
{
    public class InventoryRepository(DataContext _dataContext,
        IAuditLog _auditLog,
        IAuth _auth,
        IGPiliTerminalMachine _terminalMachine,
        ProductBarcodePDFService _pDFService) : IInventory
    {
        private static readonly HttpClient _httpClient = new()
        {
            BaseAddress = new Uri(JournalLink.Ebisx)
        };

        public async Task<Product?> GetProductByBarcode(string barcode)
        {
            return await _dataContext.Product
                .Where(p => p.IsAvailable && p.Barcode == barcode)
                .FirstOrDefaultAsync();
        }

        public async Task<Product[]> GetProducts()
        {
            return await _dataContext.Product
                .Include(p => p.Category)
                .Where(p => p.IsAvailable)
                .OrderBy(p => p.Category.CtgryName)
                .ThenBy(p => p.Name)
                .Take(30) // Limit to 30 products for performance
                .AsNoTracking()
                .ToArrayAsync();
        }

        public async Task<(bool isSuccess, string message)> RecordInventoryTransaction(string transactionType, Product product, decimal qty, string reference, User user)
        {
            if (transactionType != InventoryAction.Actions.In && transactionType != InventoryAction.Actions.Out && transactionType != InventoryAction.Actions.Adjustment)
                return (false, $"Invalid transaction type. Must be '{InventoryAction.Actions.In}', '{InventoryAction.Actions.Out}', or '{InventoryAction.Actions.Adjustment}'.");

            if (qty <= 0)
                return (false, "Quantity must be greater than zero.");

            // Adjust product quantity
            if (transactionType == InventoryAction.Actions.In)
            {
                product.Quantity = (product.Quantity ?? 0) + qty;
                // Audit log for stock in
                await _auditLog.AddManagerAudit(
                    user,
                    AuditActionType.Create,
                    $"Stock IN: {qty} units added to product '{product.Name}' (Ref: {reference})", null);
            }
            else if (transactionType == InventoryAction.Actions.Out)
            {
                if ((product.Quantity ?? 0) < qty)
                    return (false, "Not enough stock.");
                product.Quantity = (product.Quantity ?? 0) - qty;
                // Audit log for stock out
                await _auditLog.AddCashierAudit(
                    user,
                    AuditActionType.ReturnItem,
                    $"Stock OUT: {qty} units removed from product '{product.Name}' (Ref: {reference})", null);
            }
            else if (transactionType == InventoryAction.Actions.Adjustment)
            {
                product.Quantity = qty;
                // Audit log for adjustment
                await _auditLog.AddManagerAudit(
                    user,
                    AuditActionType.Update,
                    $"Stock ADJUSTMENT: Product '{product.Name}' quantity set to {qty} (Ref: {reference})", null);
            }

            // Create inventory log
            var inventory = new Inventory
            {
                Product = product,
                Quantity = qty,
                Type = transactionType, // Use the static action string
                Reference = reference,
            };

            _dataContext.Inventory.Add(inventory);
            _dataContext.Product.Update(product);
            await _dataContext.SaveChangesAsync();
            return (true, "Inventory transaction recorded successfully.");
        }

        public async Task<Product[]> SearchProducts(string keyword)
        {
            return await _dataContext.Product
                .Include(p => p.Category)
                .Where(p => p.IsAvailable &&
                    (
                        EF.Functions.Like(p.ProdId, $"%{keyword}%") ||
                        EF.Functions.Like(p.Name, $"%{keyword}%") ||
                        EF.Functions.Like(p.Barcode, $"%{keyword}%") ||
                        EF.Functions.Like(p.BaseUnit, $"%{keyword}%") ||
                        EF.Functions.Like(p.Category.CtgryName, $"%{keyword}%")
                    )
                )
                .OrderBy(p => p.Category.CtgryName)
                .ThenBy(p => p.Name)
                .Take(100)
                .ToArrayAsync();
        }

        public async Task<Inventory[]> InventoryTransactions(DateTime fromDate, DateTime toDate)
        {
            return await _dataContext.Inventory
                .Include(p => p.Product)
                .Where(d => d.TransactionDate >= fromDate && d.TransactionDate <= toDate)
                .ToArrayAsync();
        }


        public async Task<(bool isSuccess, string message)> NewProduct(Product product, string managerEmail)
        {
            if (product == null)
                return (false, "Cannot add empty product");

            // Get POS type
            var posInfo = await _terminalMachine.GetTerminalInfo();
            if (posInfo == null)
                return (false, "Terminal information not found.");
            if (posInfo.IsRetailType)
                product.ImagePath = null;
            // else: allow as provided

            // Validate required fields
            if (string.IsNullOrWhiteSpace(product.Name) ||
                string.IsNullOrWhiteSpace(product.Barcode) ||
                string.IsNullOrWhiteSpace(product.BaseUnit) ||
                string.IsNullOrWhiteSpace(product.ItemType) ||
                string.IsNullOrWhiteSpace(product.VatType) ||
                product.Category == null)
                return (false, "All product fields are required.");
            product.ProdId = product.Name;

            if (product.Cost < 0 || product.Price < 0)
                return (false, "Cost and Price must be non-negative.");

            if (product.Quantity <= 0) product.Quantity = null;

            // Check for unique barcode
            var isExisting = await _dataContext.Product
                .AnyAsync(p => p.IsAvailable && p.Barcode.ToLower().Contains(product.Barcode.ToLower()));
            if (isExisting)
                return (false, "A product with a similar barcode already exists.");

            // Validate category exists
            var category = await _dataContext.Category.FindAsync(product.Category.Id);
            if (category == null)
                return (false, "Category does not exist.");
            product.Category = category;

            var managerResult = await _auth.IsManagerValid(managerEmail);
            if (!managerResult.isSuccess || managerResult.manager == null)
                return (false, "Invalid Manager");

            _dataContext.Product.Add(product);
            await _dataContext.SaveChangesAsync();

            await _auditLog.AddManagerAudit(managerResult.manager,
                AuditActionType.Create, $"Added new product: {product.Name} (Barcode: {product.Barcode})", null);

            return (true, "Product added successfully");
        }

        public async Task<(bool isSuccess, string message)> UpdateProduct(Product product, string managerEmail)
        {
            if (product == null)
                return (false, "Cannot update empty product");

            // Get POS type
            var posInfo = await _terminalMachine.GetTerminalInfo();
            if (posInfo == null)
                return (false, "Terminal information not found.");
            if (posInfo.IsRetailType)
                product.ImagePath = null;
            // else: allow as provided

            var existing = await _dataContext.Product
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == product.Id && p.IsAvailable);
            if (existing == null)
                return (false, "Product not found.");

            // Validate required fields
            if (string.IsNullOrWhiteSpace(product.Name) ||
                string.IsNullOrWhiteSpace(product.BaseUnit) ||
                string.IsNullOrWhiteSpace(product.ItemType) ||
                string.IsNullOrWhiteSpace(product.VatType) ||
                product.Category == null)
                return (false, "All product fields are required.");
            product.ProdId = product.Name;
            product.Barcode = product.Name;

            if (product.Cost < 0 || product.Price < 0)
                return (false, "Cost and Price must be non-negative.");

            // Check for unique barcode (except self)
            var isExisting = await _dataContext.Product
                .AnyAsync(p => p.IsAvailable && p.Barcode == product.Barcode && p.Id != product.Id);
            if (isExisting)
                return (false, "A product with this barcode already exists.");

            // Validate category exists
            var category = await _dataContext.Category.FindAsync(product.Category.Id);
            if (category == null)
                return (false, "Category does not exist.");

            var managerResult = await _auth.IsManagerValid(managerEmail);
            if (!managerResult.isSuccess || managerResult.manager == null)
                return (false, "Invalid Manager");

            // Update fields
            existing.ProdId = product.ProdId;
            existing.Name = product.Name;
            existing.Barcode = product.Barcode;
            existing.BaseUnit = product.BaseUnit;
            existing.Cost = product.Cost;
            existing.Price = product.Price;
            existing.ItemType = product.ItemType;
            existing.VatType = product.VatType;
            existing.Category = category;
            existing.UpdatedAt = DateTime.Now;
            existing.ImagePath = product.ImagePath; // enforce logic above

            _dataContext.Product.Update(existing);
            await _dataContext.SaveChangesAsync();

            await _auditLog.AddManagerAudit(managerResult.manager,
                AuditActionType.Update, $"Updated product: {product.Name} (Barcode: {product.Barcode})", null);

            return (true, "Product updated successfully");
        }

        public async Task<(bool isSuccess, string message)> DeleteProduct(long id, string managerEmail)
        {
            var existing = await _dataContext.Product
                .FirstOrDefaultAsync(p => p.Id == id && p.IsAvailable);
            if (existing == null)
                return (false, "Product not found.");

            var managerResult = await _auth.IsManagerValid(managerEmail);
            if (!managerResult.isSuccess || managerResult.manager == null)
                return (false, "Invalid Manager");

            bool isReferenced = await _dataContext.Item
                .Include(p => p.Product)
                .AnyAsync(i => i.Product.Id == id);

            if (isReferenced)
            {
                existing.IsAvailable = false;
                existing.UpdatedAt = DateTime.Now;
                _dataContext.Product.Update(existing);
            }
            else
            {
                // Delete image file if exists
                if (!string.IsNullOrWhiteSpace(existing.ImagePath) && File.Exists(existing.ImagePath))
                {
                    File.Delete(existing.ImagePath);
                }
                _dataContext.Product.Remove(existing);
            }

            await _dataContext.SaveChangesAsync();

            await _auditLog.AddManagerAudit(
                managerResult.manager,
                AuditActionType.Delete,
                isReferenced
                    ? $"Soft‑deleted product: {existing.Name} (Barcode: {existing.Barcode})"
                    : $"Hard‑deleted product: {existing.Name} (Barcode: {existing.Barcode})",
                null);

            return (true, isReferenced
                ? "Product is in use, so it has been marked unavailable."
                : "Product wasn’t in use, so it was permanently deleted.");
        }


        public async Task<Category[]> GetCategories()
        {
            return await _dataContext.Category.OrderBy(c => c.CtgryName).ToArrayAsync();
        }

        public async Task<(bool isSuccess, string message)> NewCategory(Category category, string managerEmail)
        {
            if (category == null || string.IsNullOrWhiteSpace(category.CtgryName))
                return (false, "Category name is required.");

            // Check for unique name
            var isExisting = await _dataContext.Category.AnyAsync(c => c.CtgryName.ToLower() == category.CtgryName.ToLower());
            if (isExisting)
                return (false, "A category with this name already exists.");

            var managerResult = await _auth.IsManagerValid(managerEmail);
            if (!managerResult.isSuccess || managerResult.manager == null)
                return (false, "Invalid Manager");

            _dataContext.Category.Add(category);
            await _dataContext.SaveChangesAsync();

            await _auditLog.AddManagerAudit(managerResult.manager,
                AuditActionType.Create, $"Added new category: {category.CtgryName}", null);

            return (true, "Category added successfully");
        }

        public async Task<(bool isSuccess, string message)> UpdateCategory(Category category, string managerEmail)
        {
            if (category == null || string.IsNullOrWhiteSpace(category.CtgryName))
                return (false, "Category name is required.");

            var existing = await _dataContext.Category.FirstOrDefaultAsync(c => c.Id == category.Id);
            if (existing == null)
                return (false, "Category not found.");

            // Check for unique name (except self)
            var isExisting = await _dataContext.Category.AnyAsync(c => c.CtgryName.ToLower() == category.CtgryName.ToLower() && c.Id != category.Id);
            if (isExisting)
                return (false, "A category with this name already exists.");

            var managerResult = await _auth.IsManagerValid(managerEmail);
            if (!managerResult.isSuccess || managerResult.manager == null)
                return (false, "Invalid Manager");

            existing.CtgryName = category.CtgryName;
            _dataContext.Category.Update(existing);
            await _dataContext.SaveChangesAsync();

            await _auditLog.AddManagerAudit(managerResult.manager,
                AuditActionType.Update,
                $"Updated category: {category.CtgryName}", null);

            return (true, "Category updated successfully");
        }

        public async Task<(bool isSuccess, string message)> DeleteCategory(long id, string managerEmail)
        {
            var existing = await _dataContext.Category.FirstOrDefaultAsync(c => c.Id == id);
            if (existing == null)
                return (false, "Category not found.");

            // Check if category is in use
            var inUse = await _dataContext.Product.AnyAsync(p => p.Category.Id == id && p.IsAvailable);
            if (inUse)
                return (false, "Cannot delete category: it is in use by products.");

            var managerResult = await _auth.IsManagerValid(managerEmail);
            if (!managerResult.isSuccess || managerResult.manager == null)
                return (false, "Invalid Manager");

            _dataContext.Category.Remove(existing);
            await _dataContext.SaveChangesAsync();

            await _auditLog.AddManagerAudit(managerResult.manager,
                AuditActionType.Delete, $"Deleted category: {existing.CtgryName}", null);

            return (true, "Category deleted successfully");
        }

        public async Task<(bool isSuccess, string message)> LoadOnlineProducts(IProgress<(int current, int total, string status)>? progress = null)
        {
            await using var transaction = await _dataContext.Database.BeginTransactionAsync();

            try
            {
                var posInfo = await _terminalMachine.GetTerminalInfo();
                if (posInfo == null)
                    return (false, "Terminal information not found.");

                progress?.Report((0, 0, "Requesting product data from server..."));
                var response = await _httpClient.GetFromJsonAsync<LoadProductsDTO>($"asspos/mobileloaditems.php?db_name={posInfo.DbName}&usecenter={posInfo.UseCenter}");

                if (response?.Products == null || !response.Products.Any())
                    return (false, "No product items found in the API response.");

                int total = response.Products.Count;
                int processed = 0;
                int batchSize = 100;
                int batchCounter = 0;

                // 1. Categories
                progress?.Report((0, total, "Processing categories..."));
                var uniqueCategories = response.Products
                    .Select(x => x.ItemGroup?.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .GroupBy(x => x.ToLower())
                    .Select(g => g.First())
                    .ToList();

                var existingCategories = await _dataContext.Category.ToListAsync();
                int catCount = 0;
                foreach (var categoryName in uniqueCategories)
                {
                    var normalizedCategoryName = categoryName?.Trim() ?? "NA";
                    var exists = existingCategories.Any(c => c.CtgryName.ToUpper() == normalizedCategoryName.ToUpper());
                    if (!exists)
                    {
                        var category = new Category { CtgryName = normalizedCategoryName.ToUpper() };
                        await _dataContext.Category.AddAsync(category);
                        catCount++;
                    }
                }

                if (catCount > 0)
                    await _dataContext.SaveChangesAsync();

                var dbCategories = await _dataContext.Category.ToListAsync();

                // 2. Product Items
                if (total > 0)
                    progress?.Report((1, total, "Starting product import..."));
                foreach (var item in response.Products)
                {
                    processed++;
                    progress?.Report((processed, total, $"Processing {processed} of {total} items..."));

                    if (string.IsNullOrWhiteSpace(item.ItemGroup))
                        continue;

                    var category = dbCategories.FirstOrDefault(c => c.CtgryName.ToLower() == item.ItemGroup.Trim().ToLower());
                    if (category == null)
                    {
                        category = new Category { CtgryName = item.ItemGroup.Trim() };
                        await _dataContext.Category.AddAsync(category);
                        await _dataContext.SaveChangesAsync();
                        dbCategories.Add(category);
                    }

                    var existingProduct = await _dataContext.Product.FirstOrDefaultAsync(m => m.ProdId == item.ItemId);

                    string barcode;
                    if (string.IsNullOrWhiteSpace(item.Barcode))
                        barcode = item.ItemId;
                    else
                        barcode = item.Barcode;

                    if (existingProduct == null)
                    {
                        var product = new Product
                        {
                            ProdId = item.ItemId,
                            Name = item.ItemId,
                            Barcode = barcode,
                            BaseUnit = item.BaseUnit,
                            Cost = decimal.TryParse(item.Cost, out var cost) ? cost : 0,
                            Price = decimal.TryParse(item.Price, out var price) ? price : 0,
                            ItemType = item.ItemType,
                            VatType = MapVatType(item.VatType),
                            Category = category,
                            IsAvailable = true,
                        };
                        await _dataContext.Product.AddAsync(product);
                    }
                    else
                    {
                        existingProduct.Cost = decimal.TryParse(item.Cost, out var cost) ? cost : existingProduct.Cost;
                        existingProduct.Price = decimal.TryParse(item.Price, out var price) ? price : existingProduct.Price;
                        existingProduct.BaseUnit = item.BaseUnit;
                        existingProduct.ItemType = item.ItemType;
                        existingProduct.VatType = MapVatType(item.VatType);
                        existingProduct.Category = category;
                        existingProduct.Barcode = barcode;
                        existingProduct.ItemType = item.ItemId;
                    }

                    batchCounter++;
                    if (batchCounter % 100 == 0)
                    {
                        await _dataContext.SaveChangesAsync();
                        batchCounter = 0;
                    }

                    if (processed % 2 == 0)
                        await Task.Delay(1); // let UI update
                }

                await _dataContext.SaveChangesAsync(); // final save
                await transaction.CommitAsync();
                progress?.Report((total, total, "Product data loaded successfully."));
                return (true, "Product data loaded successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                progress?.Report((0, 0, $"Failed to load product data: {ex.Message}"));
                Debug.WriteLine($"Error loading product data: {ex.Message}");
                return (false, $"Failed to load product data: {ex.Message}");
            }
        }

        private static string MapVatType(string? apiVatType)
        {
            if (string.IsNullOrWhiteSpace(apiVatType))
                return VatType.Vatable;

            var vat = apiVatType.Trim().ToUpperInvariant();
            if (vat.Contains("EXEMPT"))
                return VatType.Exempt;
            if (vat.Contains("ZERO"))
                return VatType.Zero;
            return VatType.Vatable;
        }

        public async Task<(bool isSuccess, string message)> GetProductBarcodes()
        {
            var products = await _dataContext.Product
                .Where(m => m.IsAvailable)
                .ToListAsync();

            var folderPath = FolderPath.PDF.Barcodes;

            var barcodePdf = _pDFService.GenerateProductBarcodeLabels(products);

            var fileName = $"Barcodes_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";

            // Ensure directory exists
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine(folderPath, fileName);
            // Save PDF file
            await File.WriteAllBytesAsync(filePath, barcodePdf);

            return (true, $"Barcodes generated successfully: {filePath}");
        }
    }
}
