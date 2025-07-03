using Microsoft.EntityFrameworkCore;
using ServiceLibrary.Data;
using ServiceLibrary.Models;
using ServiceLibrary.Services.Interfaces;
using ServiceLibrary.Utils;

namespace ServiceLibrary.Services.Repositories
{
    public class EPaymentRepository(DataContext _dataContext,
        IAuth _auth,
        IGPiliTerminalMachine _terminalMachine,
        IAuditLog _auditLog) : IEPayment
    {
        public async Task<(bool isSuccess, string message)> AddEPayments(List<AddEPaymentsDTO> EPayments, string cashierEmail)
        {
            if (EPayments == null || !EPayments.Any())
                return (false, "No Alternative Payments Provided");

            var cashierResult = await _auth.IsCashierValid(cashierEmail);

            if (!cashierResult.isSuccess || cashierResult.cashier == null)
                return (false, "Invalid Cashier");

            var pendingOrder = await PendingOrder();
            if (pendingOrder == null)
                return (false, "No Pending Order Found");

            var saleTypeIds = EPayments.Select(e => e.SaleTypeId).Distinct().ToList();
            var saleTypes = await _dataContext.SaleType
                .Where(st => saleTypeIds.Contains(st.Id))
                .ToDictionaryAsync(st => st.Id);

            var ePayments = new List<EPayment>();

            foreach (var ePayment in EPayments)
            {
                if (!saleTypes.TryGetValue(ePayment.SaleTypeId, out var saleType))
                    return (false, $"SaleType with ID {ePayment.SaleTypeId} not found.");

                ePayments.Add(new EPayment
                {
                    Amount = ePayment.Amount,
                    Invoice = pendingOrder,
                    Reference = ePayment.Reference,
                    SaleType = saleType
                });
            }

            await _dataContext.EPayment.AddRangeAsync(ePayments);
            await _dataContext.SaveChangesAsync();

            return (true, "E-Payment/s Added Successfully");
        }

        public async Task<(bool isSuccess, string message)> AddSaleType(SaleType saleType, string managerEmail)
        {
            if (saleType == null || string.IsNullOrWhiteSpace(saleType.Name) || string.IsNullOrWhiteSpace(saleType.Account) || string.IsNullOrWhiteSpace(saleType.Type))
                return (false, "All SaleType fields are required.");

            // Check for unique name (case-insensitive)
            var isExisting = await _dataContext.SaleType.AnyAsync(s => s.Name.ToLower() == saleType.Name.ToLower());
            if (isExisting)
                return (false, "A SaleType with this name already exists.");

            var managerResult = await _auth.IsManagerValid(managerEmail);
            if (!managerResult.isSuccess || managerResult.manager == null)
                return (false, "Invalid Manager");

            _dataContext.SaleType.Add(saleType);
            await _dataContext.SaveChangesAsync();

            await _auditLog.AddManagerAudit(managerResult.manager, 
                AuditActionType.Create, $"Added new SaleType: {saleType.Name}", null);

            return (true, "SaleType added successfully");
        }

        public async Task<(bool isSuccess, string message)> DeleteSaleType(long id, string managerEmail)
        {
            var existing = await _dataContext.SaleType.FirstOrDefaultAsync(s => s.Id == id);
            if (existing == null)
                return (false, "SaleType not found.");

            var managerResult = await _auth.IsManagerValid(managerEmail);
            if (!managerResult.isSuccess || managerResult.manager == null)
                return (false, "Invalid Manager");

            _dataContext.SaleType.Remove(existing);
            await _dataContext.SaveChangesAsync();

            await _auditLog.AddManagerAudit(managerResult.manager, AuditActionType.Delete, $"Deleted SaleType: {existing.Name}", null);

            return (true, "SaleType deleted successfully");
        }

        public async Task<List<SaleType>> SaleTypes()
        {
            return await _dataContext.SaleType
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<(bool isSuccess, string message)> UpdateSaleType(SaleType saleType, string managerEmail)
        {
            if (saleType == null || string.IsNullOrWhiteSpace(saleType.Name) || string.IsNullOrWhiteSpace(saleType.Account) || string.IsNullOrWhiteSpace(saleType.Type))
                return (false, "All SaleType fields are required.");

            var existing = await _dataContext.SaleType.FirstOrDefaultAsync(s => s.Id == saleType.Id);
            if (existing == null)
                return (false, "SaleType not found.");

            // Check for unique name (except self, case-insensitive)
            var isExisting = await _dataContext.SaleType.AnyAsync(s => s.Name.ToLower() == saleType.Name.ToLower() && s.Id != saleType.Id);
            if (isExisting)
                return (false, "A SaleType with this name already exists.");

            var managerResult = await _auth.IsManagerValid(managerEmail);
            if (!managerResult.isSuccess || managerResult.manager == null)
                return (false, "Invalid Manager");

            existing.Name = saleType.Name;
            existing.Account = saleType.Account;
            existing.Type = saleType.Type;
            existing.IsActive = saleType.IsActive;

            _dataContext.SaleType.Update(existing);
            await _dataContext.SaveChangesAsync();

            await _auditLog.AddManagerAudit(managerResult.manager, 
                AuditActionType.Update, 
                $"Updated SaleType: {saleType.Name}", null);

            return (true, "SaleType updated successfully");
        }

        private async Task<Invoice?> PendingOrder()
        {
            var isTrainMode = await _terminalMachine.IsTrainMode();

            return await _dataContext.Invoice
                .Include(i => i.Items)
                .FirstOrDefaultAsync(p => p.Status == InvoiceStatusType.Pending && p.IsTrainMode == isTrainMode);
        }
    }
}
