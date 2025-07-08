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

        // Products CRUD
        Task<(bool isSuccess, string message)> NewProduct(Product product, string managerEmail);
        Task<(bool isSuccess, string message)> UpdateProduct(Product product, string managerEmail);
        Task<(bool isSuccess, string message)> DeleteProduct(long id, string managerEmail);

        // Products Category
        Task<Category[]> GetCategories();
        Task<(bool isSuccess, string message)> NewCategory(Category category, string managerEmail);
        Task<(bool isSuccess, string message)> UpdateCategory(Category category, string managerEmail);
        Task<(bool isSuccess, string message)> DeleteCategory(long id, string managerEmail);

        // Transactions
        Task<Inventory[]> InventoryTransactions(DateTime fromDate, DateTime toDate);

        // Load Products
        Task<(bool isSuccess, string message)> LoadOnlineProducts(IProgress<(int current, int total, string status)>? progress = null);

        // Print PDF

        Task<(bool isSuccess, string message)> GetProductBarcodes();

    }
}
