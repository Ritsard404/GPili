using ServiceLibrary.Services.DTO.Report;

namespace ServiceLibrary.Services.Interfaces
{
    public interface IReport
    {
        Task<InvoiceDTO?> GetInvoiceById(long invId);
    }
}
