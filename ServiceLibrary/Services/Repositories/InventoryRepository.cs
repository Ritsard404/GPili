using Microsoft.EntityFrameworkCore;
using ServiceLibrary.Data;
using ServiceLibrary.Models;
using ServiceLibrary.Services.Interfaces;
using ServiceLibrary.Utils;

namespace ServiceLibrary.Services.Repositories
{
    public class InventoryRepository(DataContext _dataContext,
        IAuditLog _auditLog,
        IAuth _auth) : IInventory
    {
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
                .Take(100) // Limit to 100 products for performance
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

            var existing = await _dataContext.Product
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == product.Id && p.IsAvailable);
            if (existing == null)
                return (false, "Product not found.");

            // Validate required fields
            if (string.IsNullOrWhiteSpace(product.Name)||
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

            _dataContext.Product.Update(existing);
            await _dataContext.SaveChangesAsync();

            await _auditLog.AddManagerAudit(managerResult.manager, 
                AuditActionType.Update, $"Updated product: {product.Name} (Barcode: {product.Barcode})", null);

            return (true, "Product updated successfully");
        }

        public async Task<(bool isSuccess, string message)> DeleteProduct(long id, string managerEmail)
        {
            var existing = await _dataContext.Product.FirstOrDefaultAsync(p => p.Id == id && p.IsAvailable);
            if (existing == null)
                return (false, "Product not found.");

            var managerResult = await _auth.IsManagerValid(managerEmail);
            if (!managerResult.isSuccess || managerResult.manager == null)
                return (false, "Invalid Manager");

            existing.IsAvailable = false;
            existing.UpdatedAt = DateTime.Now;
            _dataContext.Product.Update(existing);
            await _dataContext.SaveChangesAsync();

            await _auditLog.AddManagerAudit(managerResult.manager, 
                AuditActionType.Delete, $"Deleted product: {existing.Name} (Barcode: {existing.Barcode})", null);

            return (true, "Product deleted successfully");
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
    }
}
