using Microsoft.EntityFrameworkCore;
using ServiceLibrary.Data;
using ServiceLibrary.Models;
using ServiceLibrary.Services.DTO.Report;
using ServiceLibrary.Services.Interfaces;
using ServiceLibrary.Utils;

namespace ServiceLibrary.Services.Repositories
{
    public class ReportRepository(DataContext _dataContext, IGPiliTerminalMachine _terminalMachine) : IReport
    {
        public async Task<(string CashInDrawer, string CurrentCashDrawer)> CashTrack(string cashierEmail)
        {
            var isTrainMode = await _terminalMachine.IsTrainMode();

            var timestamp = await _dataContext.Timestamp
                .Include(t => t.Cashier)
                .Where(t => t.Cashier.Email == cashierEmail &&
                    t.TsOut == null && t.CashInDrawerAmount != null &&
                    t.CashInDrawerAmount >= 1000 && t.IsTrainMode == isTrainMode)
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
                    o.TotalAmount != 0 && o.IsTrainMode == isTrainMode)
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

        public async Task<XInvoiceDTO> GetXInvoice()
        {
            decimal defaultDecimal = 0m;
            DateTime defaultDate = DateTime.UtcNow;

            var isTrainMode = await _terminalMachine.IsTrainMode();

            var posInfo = await _terminalMachine.GetTerminalInfo();

            var orders = await _dataContext.Invoice
                .Include(o => o.Cashier)
                .Include(o => o.Items)
                .Include(o => o.EPayments)
                    .ThenInclude(ap => ap.SaleType)
                .Where(o => !o.IsRead && o.IsTrainMode == isTrainMode)
                .ToListAsync();


            var ts = await _dataContext.Timestamp
                .Include(t => t.Cashier)
                .OrderBy(t => t.Id)
                .LastOrDefaultAsync(o => o.IsTrainMode == isTrainMode);

            // Handle empty orders scenario
            var firstOrder = orders.FirstOrDefault();
            var lastOrder = orders.LastOrDefault();
            var orderCount = orders.Count().ToString();

            // Calculate financials with null protection
            decimal openingFundDec = ts?.CashInDrawerAmount ?? defaultDecimal;

            // Move withdrawal calculation to memory
            var withdrawnAmount = ts?.WithdrawnDrawerAmount;

            // Calculate void and refund amounts in memory
            decimal voidDec = orders.Where(o => o.Status == InvoiceStatusType.Void)
                                  .Sum(o => o?.TotalAmount ?? defaultDecimal);
            string voidCount = orders.Count(o => o.Status == InvoiceStatusType.Void).ToString();

            decimal refundDec = orders.Where(o => o.Status == InvoiceStatusType.Returned)
                                    .Sum(o => o?.ReturnedAmount ?? defaultDecimal);
            string refundCount = orders.Count(o => o.Status == InvoiceStatusType.Returned).ToString();

            // Calculate valid orders total in memory - Now considering ReturnedAmount
            decimal validOrdersTotal = orders.Where(o => o.Status == InvoiceStatusType.Paid)
                                           .Sum(o => (o?.CashTendered ?? defaultDecimal) -
                                                   (o?.ChangeAmount ?? defaultDecimal) -
                                                   (o?.ReturnedAmount ?? defaultDecimal));

            decimal actualCash = openingFundDec + validOrdersTotal;
            decimal expectedCash = ts?.CashOutDrawerAmount ?? defaultDecimal + withdrawnAmount ?? defaultDecimal;
            decimal shortOverDec = (expectedCash - actualCash) - refundDec;

            // Safe payment processing - Adjusted for ReturnedAmount
            var payments = new Payments
            {
                Cash = orders.Sum(o => (o?.CashTendered ?? defaultDecimal) -
                                      (o?.ChangeAmount ?? defaultDecimal) -
                                      (o?.ReturnedAmount ?? defaultDecimal)),
                OtherPayments = orders
                    .SelectMany(o => o.EPayments)
                    .GroupBy(ap => ap.SaleType?.Name ?? "Unknown")
                    .Select(g => new PaymentDetail
                    {
                        Name = g.Key + $" ({g.Count()}) :",
                        Amount = g.Sum(x => x.Amount * (1 - ((x.Invoice?.ReturnedAmount ?? defaultDecimal) /
                            (x.Invoice?.TotalAmount > 0 ? x.Invoice.TotalAmount : 1m)))),
                    }).ToList()
            };

            var summary = new TransactionSummary
            {
                CashInDrawer = (ts?.CashOutDrawerAmount ?? defaultDecimal).PesoFormat(),
                OtherPayments = payments.OtherPayments
            };

            // Build DTO with safe values
            var dto = new XInvoiceDTO
            {
                BusinessName = posInfo.RegisteredName ?? "N/A",
                OperatorName = posInfo.OperatedBy ?? "N/A",
                AddressLine = posInfo.Address ?? "N/A",
                VatRegTin = posInfo.VatTinNumber ?? "N/A",
                Min = posInfo.MinNumber ?? "N/A",
                SerialNumber = posInfo.PosSerialNumber ?? "N/A",

                ReportDate = DateTime.Now.ToString("MMMM dd, yyyy"),
                ReportTime = DateTime.Now.ToString("hh:mm tt"),
                StartDateTime = firstOrder?.CreatedAt.ToString("MM/dd/yy hh:mm tt")
                              ?? defaultDate.ToString("MM/dd/yy hh:mm tt"),
                EndDateTime = lastOrder?.CreatedAt.ToString("MM/dd/yy hh:mm tt")
                             ?? defaultDate.ToString("MM/dd/yy hh:mm tt"),

                Cashier = ts?.Cashier != null
                        ? $"{ts.Cashier.FName} {ts.Cashier.LName}"
                        : "N/A",
                BeginningOrNumber = firstOrder?.InvoiceNumber.ToString("D12") ?? "N/A",
                EndingOrNumber = lastOrder?.InvoiceNumber.ToString("D12") ?? "N/A",
                TransactCount = orderCount,

                OpeningFund = openingFundDec.PesoFormat(),
                VoidAmount = voidDec.PesoFormat(),
                VoidCount = voidCount,
                Refund = refundDec.PesoFormat(),
                RefundCount = refundCount,
                Withdrawal = withdrawnAmount.PesoFormat(),

                Payments = payments,
                TransactionSummary = summary,
                ShortOver = shortOverDec.PesoFormat()
            };

            // Mark orders as read if any exist
            if (orders.Any())
            {
                foreach (var order in orders)
                {
                    order.IsRead = true;
                }
                await _dataContext.SaveChangesAsync();
            }

            return dto;
        }

        public async Task<ZInvoiceDTO> GetZInvoice()
        {
            var defaultDecimal = 0m;
            var today = DateTime.Today;
            var defaultDate = today;

            var isTrainMode = await _terminalMachine.IsTrainMode();

            var posInfo = await _terminalMachine.GetTerminalInfo();

            var orders = await _dataContext.Invoice
                .Where(o => o.IsTrainMode == isTrainMode)
                .Include(o => o.Items)
                .Include(o => o.EPayments)
                    .ThenInclude(ap => ap.SaleType)
                .ToListAsync();

            // Initialize empty collections to prevent null references
            var allTimestamps = await _dataContext.Timestamp
                .Where(t => t.IsTrainMode == isTrainMode)
                .Include(t => t.Cashier)
                .ToListAsync(); // Handle empty scenario for dates
            var startDate = orders.Any() ? orders.Min(t => t.CreatedAt) : DateTime.Now;
            var endDate = orders.Any() ? orders.Max(t => t.CreatedAt) : DateTime.Now;

            // Withdrawal Amount
            var withdrawnAmount = allTimestamps
                .Where(t => t.TsOut.HasValue
                    && t.TsOut.Value.Date == today)
                .Sum(mw => mw?.WithdrawnDrawerAmount);

            var allRegularOrders = orders.Where(o => o.Status == InvoiceStatusType.Paid).ToList();
            var allVoidOrders = orders.Where(o => o.Status == InvoiceStatusType.Void).ToList();
            var allReturnOrders = orders.Where(o => o.Status == InvoiceStatusType.Returned).ToList();

            // BREAKDOWN OF SALES
            var regularOrders = allRegularOrders.Where(o => o.CreatedAt.Date == today).ToList();
            var voidOrders = allVoidOrders.Where(o => o.CreatedAt.Date == today).ToList();
            var returnOrders = allReturnOrders.Where(o => o.CreatedAt.Date == today).ToList();

            // Accumulated Sales - Now considering ReturnedAmount
            decimal salesForTheDay = regularOrders
                .Where(c => c.CreatedAt.Date == today)
                .Sum(o => o.TotalAmount - (o.DiscountAmount ?? defaultDecimal) - (o.ReturnedAmount ?? defaultDecimal));

            decimal previousAccumulatedSales = allRegularOrders
                .Where(c => c.CreatedAt.Date < today)
                .Sum(o => o.TotalAmount - (o.DiscountAmount ?? defaultDecimal) - (o.ReturnedAmount ?? defaultDecimal));

            decimal presentAccumulatedSales = previousAccumulatedSales + salesForTheDay;

            throw new NotImplementedException();
        }
    }
}
