using ServiceLibrary.Models;
using ServiceLibrary.Services.DTO.Report;
using ServiceLibrary.Services.Interfaces;

namespace ServiceLibrary.Services.Repositories
{
    public class OrderRepository : IOrder
    {
        public Task<(bool isSuccess, string message)> AddOrderItem(Product product)
        {
            throw new NotImplementedException();
        }

        public Task<(bool isSuccess, string message)> EditQtyTotalPriceItem(Item item)
        {
            throw new NotImplementedException();
        }

        public Task<List<Item>> GetPendingItems()
        {
            throw new NotImplementedException();
        }

        public Task<List<Product>> GetProducts()
        {
            throw new NotImplementedException();
        }

        public Task<(bool isSuccess, string message, InvoiceDTO invoiceInfo)> PayOrder()
        {
            throw new NotImplementedException();
        }

        public Task<(bool isSuccess, string message)> VoidItem(Item item)
        {
            throw new NotImplementedException();
        }

        public Task<(bool isSuccess, string message)> VoidOrder()
        {
            throw new NotImplementedException();
        }
    }
}
