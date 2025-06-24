using Microsoft.EntityFrameworkCore;
using ServiceLibrary.Data;
using ServiceLibrary.Models;
using ServiceLibrary.Services.Interfaces;

namespace ServiceLibrary.Services.Repositories
{
    public class InventoryRepository(DataContext _dataContext) : IInventory
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
