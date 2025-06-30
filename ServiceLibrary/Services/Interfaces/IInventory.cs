using ServiceLibrary.Models;

namespace ServiceLibrary.Services.Interfaces
{
    public interface IInventory
    {
        Task<Product[]> GetProducts();
        Task<Product[]> SearchProducts(string keyword);
        Task<Product?> GetProductByBarcode(string barcode); 
        Task<(bool isSuccess, string message)> RecordInventoryTransaction(string transactionType, 
            Product product, decimal qty, string reference, User user);
    }
}
