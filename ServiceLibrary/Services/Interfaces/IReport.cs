using ServiceLibrary.Services.DTO.Report;

namespace ServiceLibrary.Services.Interfaces
{
    public interface IReport
    {
        Task<InvoiceDTO?> GetInvoiceById(long invId);
        Task<(string CashInDrawer, string CurrentCashDrawer)> CashTrack(string cashierEmail);
        Task<XInvoiceDTO> GetXInvoice();
        Task<ZInvoiceDTO> GetZInvoice();
    }
}
