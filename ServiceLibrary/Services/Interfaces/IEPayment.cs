using ServiceLibrary.Models;

namespace ServiceLibrary.Services.Interfaces
{
    public interface IEPayment
    {
        Task<List<SaleType>> SaleTypes(); 
        Task<(bool isSuccess, string message)> AddEPayments(List<AddEPaymentsDTO> EPayments, string cashierEmail);

        Task<(bool isSuccess, string message)> AddSaleType(SaleType saleType, string managerEmail);
        Task<(bool isSuccess, string message)> UpdateSaleType(SaleType saleType, string managerEmail);
        Task<(bool isSuccess, string message)> DeleteSaleType(long id, string managerEmail);
    }

    public class AddEPaymentsDTO
    {
        public required string Reference { get; set; }
        public required decimal Amount { get; set; }
        public required int SaleTypeId { get; set; }
    }
}
