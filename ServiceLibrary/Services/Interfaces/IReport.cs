using ServiceLibrary.Models;
using ServiceLibrary.Services.DTO.Report;

namespace ServiceLibrary.Services.Interfaces
{
    public interface IReport
    {
        Task<InvoiceDTO?> GetInvoiceById(long invId);
        Task<(string CashInDrawer, string CurrentCashDrawer, string CashierName)> CashTrack(string cashierEmail);
        Task<XInvoiceDTO> GetXInvoice();
        Task<ZInvoiceDTO> GetZInvoice();

        Task<List<GetInvoiceDocumentDTO>> InvoiceDocuments(DateTime fromDate, DateTime toDate);
    }
}
