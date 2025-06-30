using Microsoft.EntityFrameworkCore;
using ServiceLibrary.Data;
using ServiceLibrary.Models;
using ServiceLibrary.Services.Interfaces;
using ServiceLibrary.Utils;

namespace ServiceLibrary.Services.Repositories
{
    public class InventoryRepository(DataContext _dataContext, IAuditLog _auditLog) : IInventory
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
                    AuditActionType.AddItem,
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
                    AuditActionType.UpdateItem,
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
    }
}
