using ServiceLibrary.Models;

namespace ServiceLibrary.Services.Interfaces
{
    public interface IEPayment
    {
        Task<List<SaleType>> SaleTypes(); 
        Task<(bool isSuccess, string message)> AddEPayments(List<AddEPaymentsDTO> EPayments, string cashierEmail);

    }

    public class AddEPaymentsDTO
    {
        public required string Reference { get; set; }
        public required decimal Amount { get; set; }
        public required int SaleTypeId { get; set; }
    }
}
