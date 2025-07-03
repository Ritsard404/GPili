using Microsoft.EntityFrameworkCore;
using ServiceLibrary.Data;
using ServiceLibrary.Services.DTO.Report;
using ServiceLibrary.Services.Interfaces;
using ServiceLibrary.Utils;

namespace ServiceLibrary.Services.Repositories
{
    public class ReportRepository(DataContext _dataContext, IGPiliTerminalMachine _terminalMachine) : IReport
    {
        public async Task<(string CashInDrawer, string CurrentCashDrawer)> CashTrack(string cashierEmail)
        {
            var timestamp = await _dataContext.Timestamp
                .Include(t => t.Cashier)
                .Where(t => t.Cashier.Email == cashierEmail &&
                    t.TsOut == null && t.CashInDrawerAmount != null &&
                    t.CashInDrawerAmount >= 1000)
                .FirstOrDefaultAsync();

            if (timestamp == null || timestamp.CashInDrawerAmount == null)
                return ("₱0.00", "₱0.00");

            var tsIn = timestamp.TsIn;

            // Get withdrawals
            var withdrawals = timestamp.WithdrawnDrawerAmount;

            // Get cash in drawer
            var cashInDrawer = timestamp.CashInDrawerAmount;

            // Fetch all orders with their cashier
            var orders = await _dataContext.Invoice
                .Include(o => o.Cashier)
                .ToListAsync();

            // Filter and calculate in memory
            decimal totalCashInDrawer = orders
                .Where(o =>
                    o.Cashier.Email == cashierEmail &&
                    o.Status == InvoiceStatusType.Paid &&
                    o.CreatedAt >= tsIn &&
                    o.CashTendered != null &&
                    o.TotalAmount != 0)
                .Sum(o => o.CashTendered ?? 0m - o.ChangeAmount ?? 0m - o.ReturnedAmount ?? 0m);

            decimal currentCashDrawer = totalCashInDrawer + cashInDrawer ?? 0m - withdrawals ?? 0m;

            return (cashInDrawer.PesoFormat(), currentCashDrawer.PesoFormat());
        }

        public async Task<InvoiceDTO?> GetInvoiceById(long invId)
        {
            var order = await _dataContext.Invoice
                .Include(i => i.Items)
                    .ThenInclude(i => i.Product)
                .Include(i => i.Cashier)
                .Include(i => i.EPayments)
                    .ThenInclude(ap => ap.SaleType)
                .FirstOrDefaultAsync(i => i.Id == invId);

            if (order == null)
                return null;

            var terminalInfo = await _terminalMachine.GetTerminalInfo();
            if (terminalInfo == null)
                return null;

            var businessInfo = new BusinesDetails
            {
                PosSerialNumber = terminalInfo.PosSerialNumber,
                MinNumber = terminalInfo.MinNumber,
                AccreditationNumber = terminalInfo.AccreditationNumber,
                PtuNumber = terminalInfo.PtuNumber,
                DateIssued = terminalInfo.DateIssued.DateFormat(),
                ValidUntil = terminalInfo.ValidUntil.DateFormat(),
                PosName = terminalInfo.PosName,
                RegisteredName = terminalInfo.RegisteredName,
                OperatedBy = terminalInfo.OperatedBy,
                Address = terminalInfo.Address,
                VatTinNumber = terminalInfo.VatTinNumber,
                CostCenter = terminalInfo.CostCenter,
                BranchCenter = terminalInfo.BranchCenter,
                IsTrainMode = terminalInfo.IsTrainMode,
            };

            var items = order.Items.Select(item => new ItemInfo
            {
                Qty = item.QtyDisplay,
                Description = item.DisplayNameWithPrice.Length > 20
                    ? item.DisplayNameWithPrice.Substring(0, 20)
                    : item.DisplayNameWithPrice,
                Amount = item.DisplaySubtotalVat,
            }).ToList();

            var otherPayments = order.EPayments.Select(ap => new OtherPayment
            {
                SaleTypeName = ap.SaleType.Name,
                Reference = ap.Reference,
                Amount = ap.Amount.PesoFormat(),
            }).ToList();

            var invoice = new InvoiceDTO
            {
                BusinesDetails = businessInfo,
                InvoiceNum = order.InvoiceNumber.InvoiceFormat(),
                InvoiceDate = order.CreatedAt.DateFormat(),
                CashierName = order.Cashier?.FullName ?? "Unknown",
                Items = items,
                TotalAmount = order.TotalAmount.PesoFormat(),
                SubTotal = order.SubTotal.PesoFormat(),
                DiscountAmount = order.DiscountAmount.PesoFormat(),
                DueAmount = order.DueAmount.PesoFormat(),
                OtherPayments = otherPayments,
                CashTenderAmount = order.CashTendered.PesoFormat(),
                TotalTenderAmount = order.TotalTendered.PesoFormat(),
                ChangeAmount = order.ChangeAmount.PesoFormat(),
                VatExemptSales = order.VatExempt.PesoFormat(),
                VatSales = order.VatSales.PesoFormat(),
                VatAmount = order.VatAmount.PesoFormat(),
                VatZero = order.VatZero.PesoFormat(),
                ElligiblePersonDiscount = order.EligibleDiscName?.ToUpper(),
                PrintCount = order.PrintCount.ToString()
            };

            return invoice;
        }

        public Task<XInvoiceDTO> GetXInvoice()
        {
            throw new NotImplementedException();
        }

        public Task<ZInvoiceDTO> GetZInvoice()
        {
            throw new NotImplementedException();
        }
    }
}
