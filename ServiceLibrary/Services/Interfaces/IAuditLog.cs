using ServiceLibrary.Models;

namespace ServiceLibrary.Services.Interfaces
{
    public interface IAuditLog
    {
        Task<(bool isSuccess, string message)> AddManagerAudit(User manager, string action, string changes, decimal? amount);
        Task<(bool isSuccess, string message)> AddCashierAudit(User cashier, string action, string changes, decimal? amount);

        Task<(bool isSuccess, string message)> AddPwdScJournal(long invId);
        Task<(bool isSuccess, string message)> AddItemsJournal(long invId);
        Task<(bool isSuccess, string message)> AddTendersJournal(long invId);
        Task<(bool isSuccess, string message)> AddTotalsJournal(long invId);

        Task<(bool isSuccess, string message)> PushJournals(DateTime selectedDate, IProgress<(int current, int total, string status)>? progress = null);
    }
}
