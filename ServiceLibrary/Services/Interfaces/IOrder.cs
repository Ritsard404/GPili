using ServiceLibrary.Models;
using ServiceLibrary.Services.DTO.Order;
using ServiceLibrary.Services.DTO.Report;

namespace ServiceLibrary.Services.Interfaces
{
    public interface IOrder
    {

        Task<(bool isSuccess, string message)> AddOrderItem(long prodId, decimal qty, string cashierEmail);
        Task<(bool isSuccess, string message)> EditQtyTotalPriceItem(long itemId, decimal qty, decimal subtotal);
        Task<(bool isSuccess, string message)> VoidItem(string mgrEmail, string cashrEmail, long itemId);
        Task<(bool isSuccess, string message, InvoiceDTO? invoiceInfo)> PayOrder(PayOrderDTO pay);
        Task<(bool isSuccess, string message)> VoidOrder(string cashierEmail, string managerEmail, string reason);


        Task<List<Item>> GetToRefundItems(long invNum);
        Task<(bool isSuccess, string message)> ReturnInvoice(string managerEmail, long invoiceNumber);
        Task<(bool isSuccess, string message)> ReturnItems(string managerEmail, long invoiceNumber, List<Item> items, string reason);

        Task<List<Item>> GetPendingItems();
    }
}
