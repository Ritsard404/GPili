using ServiceLibrary.Data;
using ServiceLibrary.Models;
using ServiceLibrary.Services.Interfaces;

namespace ServiceLibrary.Services.Repositories
{
    public class AuditLogRepository(DataContext _dataContext) : IAuditLog
    {
        public async Task<(bool isSuccess, string message)> AddCashierAudit(User cashier, string action, string changes, decimal? amount)
        {
            _dataContext.AuditLog.Add(new AuditLog
            {
                Cashier = cashier,
                Action = action,
                Changes = changes,
                Amount = amount,
            });

            await _dataContext.SaveChangesAsync();
            return (true, "Cashier audit added successfully.");
        }

        public Task<(bool isSuccess, string message)> AddItemsJournal(long invId)
        {
            throw new NotImplementedException();
        }

        public async Task<(bool isSuccess, string message)> AddManagerAudit(User manager, string action, string changes, decimal? amount)
        {
            _dataContext.AuditLog.Add(new AuditLog
            {
                Manager= manager,
                Action = action,
                Changes = changes,
                Amount = amount,
            });

            await _dataContext.SaveChangesAsync();
            return (true, "Manager audit added successfully.");
        }

        public Task<(bool isSuccess, string message)> AddPwdScJournal(long invId)
        {
            throw new NotImplementedException();
        }

        public Task<(bool isSuccess, string message)> AddTendersJournal(long invId)
        {
            throw new NotImplementedException();
        }

        public Task<(bool isSuccess, string message)> AddTotalsJournal(long invId)
        {
            throw new NotImplementedException();
        }
    }
}
