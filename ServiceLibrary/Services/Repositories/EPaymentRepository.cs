using Microsoft.EntityFrameworkCore;
using ServiceLibrary.Data;
using ServiceLibrary.Models;
using ServiceLibrary.Services.Interfaces;
using ServiceLibrary.Utils;

namespace ServiceLibrary.Services.Repositories
{
    public class EPaymentRepository(DataContext _dataContext,
        IAuth _auth,
        IGPiliTerminalMachine _terminalMachine) : IEPayment
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

        public async Task<List<SaleType>> SaleTypes()
        {
            return await _dataContext.SaleType
                .AsNoTracking()
                .ToListAsync();
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
