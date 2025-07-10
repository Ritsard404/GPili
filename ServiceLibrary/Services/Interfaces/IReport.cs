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

        Task<(List<TransactionListDTO> Data, TotalTransactionListDTO Totals, string FilePath)> 
            GetTransactList(DateTime fromDate, DateTime toDate);
        Task<string> GetAuditTrail(DateTime fromDate, DateTime toDate);
        Task<string> GetSalesReport(DateTime fromDate, DateTime toDate);
        Task<string> GetSalesBook(DateTime fromDate, DateTime toDate);
        Task<string> GetVoidedListsReport(DateTime fromDate, DateTime toDate);
        Task<(List<TransactionListDTO> Data, TotalTransactionListDTO Totals, string FilePath)> 
            GetPwdOrSeniorList(DateTime fromDate, DateTime toDate, string type);
    }
}
