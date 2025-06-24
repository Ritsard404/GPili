using ServiceLibrary.Models;

namespace ServiceLibrary.Services.Interfaces
{
    public interface IInventory
    {
        Task<Product[]> GetProducts();
        Task<Product[]> SearchProducts(string keyword);
        Task<Product?> GetProductByBarcode(string barcode);
    }
}
