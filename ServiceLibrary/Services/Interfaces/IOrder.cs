using ServiceLibrary.Models;
using ServiceLibrary.Services.DTO.Report;

namespace ServiceLibrary.Services.Interfaces
{
    public interface IOrder
    {
        Task<List<Product>> GetProducts();

        Task<(bool isSuccess, string message)> AddOrderItem(Product product);
        Task<(bool isSuccess, string message)> EditQtyTotalPriceItem(Item item);
        Task<(bool isSuccess, string message)> VoidItem(Item item);
        Task<(bool isSuccess, string message, InvoiceDTO invoiceInfo)> PayOrder();
        Task<(bool isSuccess, string message)> VoidOrder();

        Task<List<Item>> GetPendingItems();
    }
}
