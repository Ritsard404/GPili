using EBISX_POS.API.Services.PDF;
using Microsoft.EntityFrameworkCore;
using ServiceLibrary.Data;
using ServiceLibrary.Models;
using ServiceLibrary.Services.DTO.Report;
using ServiceLibrary.Services.Interfaces;
using ServiceLibrary.Services.PDF;
using ServiceLibrary.Utils;
using System.Globalization;

namespace ServiceLibrary.Services.Repositories
{
    public class ReportRepository(DataContext _dataContext, IGPiliTerminalMachine _terminalMachine,
        TransactionListPDFService _pDFService, AuditTrailPDFService _auditTrailPDFService,
        SalesReportPDFService _salesReportPDFService, VoidedListPDFService _voidedListPDFService) : IReport
    {
        public async Task<(string CashInDrawer, string CurrentCashDrawer, string CashierName)> CashTrack(string cashierEmail)
        {
            var isTrainMode = await _terminalMachine.IsTrainMode();

            var timestamp = await _dataContext.Timestamp
                .Include(t => t.Cashier)
                .Where(t => t.Cashier.Email == cashierEmail &&
                    t.TsOut == null && t.CashInDrawerAmount != null &&
                    t.IsTrainMode == isTrainMode)
                .FirstOrDefaultAsync();

            if (timestamp == null || timestamp.CashInDrawerAmount == null)
                return ("₱0.00", "₱0.00", "");

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
                     o.IsTrainMode == isTrainMode)
                 .Sum(o =>
                     o.CashTendered.GetValueOrDefault()
                     - o.ChangeAmount.GetValueOrDefault()
                     - o.ReturnedAmount.GetValueOrDefault()
                 );

            decimal currentCashDrawer = totalCashInDrawer
                + cashInDrawer.GetValueOrDefault()
                - withdrawals.GetValueOrDefault();

            return (cashInDrawer.PesoFormat(), currentCashDrawer.PesoFormat(), timestamp.Cashier.FullName);
        }

        public async Task<InvoiceDTO?> GetInvoiceById(long invId)
        {
            var order = await _dataContext.Invoice
                .Include(i => i.Items)
                    .ThenInclude(i => i.Product)
                .Include(i => i.Cashier)
                .Include(i => i.EPayments)
                    .ThenInclude(ap => ap.SaleType)
                .AsNoTracking()
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
                DateIssued = terminalInfo.DateIssued.DateTimeFormat(),
                ValidUntil = terminalInfo.ValidUntil.DateTimeFormat(),
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
                IsReturned = order.Status == InvoiceStatusType.Returned,
                BusinesDetails = businessInfo,
                InvoiceNum = order.InvoiceNumber.InvoiceFormat(),
                InvoiceDate = order.CreatedAt.DateTimeFormat(),
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
                DiscountType = order.DiscountType == DiscountType.SeniorCitizen
                        ? "Senior"
                        : order.DiscountType
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

            decimal expectedCash = openingFundDec + validOrdersTotal - withdrawnAmount.GetValueOrDefault();
            decimal actualCash = ts?.CashOutDrawerAmount ?? defaultDecimal;
            decimal shortOverDec = (actualCash - expectedCash) - refundDec;

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
                ShortOver = shortOverDec.PesoFormat(),
                IsTrainMode = isTrainMode
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

            // Financial calculations with default values and ReturnedAmount
            decimal grossSales = regularOrders.Sum(o => o?.GrossAmount ?? defaultDecimal);
            decimal totalVoid = voidOrders.Sum(o => o?.TotalAmount ?? defaultDecimal);
            decimal totalReturns = returnOrders.Sum(o => o?.ReturnedAmount ?? defaultDecimal);
            decimal totalDiscounts = regularOrders.Sum(o => o?.DiscountAmount ?? defaultDecimal);
            decimal cashSales = regularOrders.Sum(o =>
                (o?.CashTendered ?? defaultDecimal) -
                (o?.ChangeAmount ?? defaultDecimal) -
                (o?.ReturnedAmount ?? defaultDecimal));

            decimal netAmount = grossSales - totalReturns - totalVoid - totalDiscounts;

            // VAT calculations with defaults - Adjusted for ReturnedAmount
            decimal vatableSales = regularOrders.Sum(v =>
                (v?.VatSales ?? defaultDecimal) *
                (1 - ((v?.ReturnedAmount ?? defaultDecimal) / (v?.TotalAmount ?? 1m))));

            decimal vatAmount = regularOrders.Sum(o =>
                (o?.VatAmount ?? defaultDecimal) *
                (1 - ((o?.ReturnedAmount ?? defaultDecimal) / (o?.TotalAmount ?? 1m))));

            decimal vatExempt = regularOrders.Sum(o =>
                (o?.VatExempt ?? defaultDecimal) *
                (1 - ((o?.ReturnedAmount ?? defaultDecimal) / (o?.TotalAmount ?? 1m))));

            decimal zeroRated = 0m;

            // Cash in Drawer
            decimal cashInDrawer = allTimestamps
                .Where(t => t.TsOut.HasValue
                    && t.TsOut.Value.Date == today)
                .Sum(s => s.CashOutDrawerAmount) ?? defaultDecimal;

            // Opening Fund
            decimal openingFund = allTimestamps
                .Where(t => t.TsIn.Value.Date == today)
                .Sum(s => s.CashInDrawerAmount) ?? defaultDecimal;

            decimal actualCash = openingFund + cashSales;
            decimal expectedCash = cashInDrawer + withdrawnAmount ?? defaultDecimal;
            decimal shortOver = (expectedCash - actualCash) - totalReturns;

            // Discount calculations adjusted for ReturnedAmount
            decimal seniorDiscount = regularOrders
                .Where(s => s.DiscountType == DiscountType.SeniorCitizen)
                .Sum(s => (s.DiscountAmount ?? 0m) *
                    (1 - ((s.ReturnedAmount ?? 0m) / (s.TotalAmount > 0 ? s.TotalAmount : 1m))));

            string seniorCount = regularOrders
                .Where(s => s.DiscountType == DiscountType.SeniorCitizen)
                .Count()
                .ToString();

            decimal pwdDiscount = regularOrders
                .Where(s => s.DiscountType == DiscountType.Pwd)
                .Sum(s => (s.DiscountAmount ?? 0m) *
                    (1 - ((s.ReturnedAmount ?? 0m) / (s.TotalAmount > 0 ? s.TotalAmount : 1m))));

            string pwdCount = regularOrders
                .Where(s => s.DiscountType == DiscountType.Pwd)
                .Count()
                .ToString();

            decimal otherDiscount = regularOrders
                .Where(s => s.DiscountType == DiscountType.Others)
                .Sum(s => (s.DiscountAmount ?? 0m) *
                    (1 - ((s.ReturnedAmount ?? 0m) / (s.TotalAmount > 0 ? s.TotalAmount : 1m))));

            string otherCount = regularOrders
                .Where(s => s.DiscountType == DiscountType.Others)
                .Count()
                .ToString();

            // Safe payment processing - Adjusted for ReturnedAmount
            var payments = new Payments
            {
                Cash = cashSales,
                OtherPayments = orders
                .SelectMany(o => o.EPayments != null && o.CreatedAt.Date == today ? o.EPayments : new List<EPayment>())
                .GroupBy(ap => ap.SaleType?.Name ?? "Unknown")
                .Select(g => new PaymentDetail
                {
                    Name = g.Key + $" ({g.Count()}):",
                    Amount = g.Sum(x => x.Amount * (1 - ((x.Invoice?.ReturnedAmount ?? defaultDecimal) / (x.Invoice?.TotalAmount ?? 1m)))),
                }).ToList()
            };

            // Build DTO with zero defaults
            var dto = new ZInvoiceDTO
            {
                BusinessName = posInfo.RegisteredName ?? "N/A",
                OperatorName = posInfo.OperatedBy ?? "N/A",
                AddressLine = posInfo.Address ?? "N/A",
                VatRegTin = posInfo.VatTinNumber ?? "N/A",
                Min = posInfo.MinNumber ?? "N/A",
                SerialNumber = posInfo.PosSerialNumber ?? "N/A",

                ReportDate = DateTime.Now.ToString("MMMM dd, yyyy"),
                ReportTime = DateTime.Now.ToString("hh:mm tt"),
                StartDateTime = startDate.ToString("MM/dd/yy hh:mm tt"),
                EndDateTime = endDate.ToString("MM/dd/yy hh:mm tt"),

                // Order numbers
                BeginningSI = GetOrderNumber(orders.Min(o => o?.InvoiceNumber)),
                EndingSI = GetOrderNumber(orders.Max(o => o?.InvoiceNumber)),
                BeginningVoid = GetOrderNumber(allVoidOrders.Min(o => o?.InvoiceNumber)),
                EndingVoid = GetOrderNumber(allVoidOrders.Max(o => o?.InvoiceNumber)),
                BeginningReturn = GetOrderNumber(allReturnOrders.Min(o => o?.InvoiceNumber)),
                EndingReturn = GetOrderNumber(allReturnOrders.Max(o => o?.InvoiceNumber)),
                TransactCount = orders.Count().ToString(),

                // Always zero when empty
                ResetCounter = isTrainMode ? posInfo.ResetCounterTrainNo.ToString() : posInfo.ResetCounterNo.ToString(),
                ZCounter = isTrainMode ? posInfo.ZCounterTrainNo.ToString() : posInfo.ZCounterNo.ToString(),

                // Financial summaries - Now using ReturnedAmount
                PresentAccumulatedSales = presentAccumulatedSales.PesoFormat(),
                PreviousAccumulatedSales = previousAccumulatedSales.PesoFormat(),
                SalesForTheDay = salesForTheDay.PesoFormat(),

                SalesBreakdown = new SalesBreakdown
                {
                    VatableSales = vatableSales.PesoFormat(),
                    VatAmount = vatAmount.PesoFormat(),
                    VatExemptSales = vatExempt.PesoFormat(),
                    ZeroRatedSales = zeroRated.PesoFormat(),
                    GrossAmount = grossSales.PesoFormat(),
                    LessDiscount = totalDiscounts.PesoFormat(),
                    LessReturn = totalReturns.PesoFormat(),
                    LessVoid = totalVoid.PesoFormat(),
                    LessVatAdjustment = defaultDecimal.PesoFormat(),
                    NetAmount = netAmount.PesoFormat()
                },

                TransactionSummary = new TransactionSummary
                {
                    CashInDrawer = cashInDrawer.PesoFormat(),
                    OtherPayments = payments.OtherPayments
                },

                DiscountSummary = new DiscountSummary
                {
                    SeniorCitizen = seniorDiscount.PesoFormat(),
                    SeniorCitizenCount = seniorCount,
                    PWD = pwdDiscount.PesoFormat(),
                    PWDCount = pwdCount,
                    Other = otherDiscount.PesoFormat(),
                    OtherCount = otherCount
                },

                SalesAdjustment = new SalesAdjustment
                {
                    Return = totalReturns.PesoFormat(),
                    ReturnCount = returnOrders.Count().ToString(),
                    Void = totalVoid.PesoFormat(),
                    VoidCount = voidOrders.Count().ToString(),
                },

                VatAdjustment = new VatAdjustment
                {
                    SCTrans = defaultDecimal.PesoFormat(),
                    PWDTrans = defaultDecimal.PesoFormat(),
                    RegDiscTrans = defaultDecimal.PesoFormat(),
                    ZeroRatedTrans = defaultDecimal.PesoFormat(),
                    VatOnReturn = defaultDecimal.PesoFormat(),
                    OtherAdjustments = defaultDecimal.PesoFormat()
                },

                OpeningFund = openingFund.PesoFormat(),
                Withdrawal = withdrawnAmount.PesoFormat(),
                PaymentsReceived = (cashSales + payments.OtherPayments.Sum(s => s.Amount)).PesoFormat(),
                ShortOver = shortOver.PesoFormat()
            };

            if (isTrainMode)
            {
                posInfo.ZCounterTrainNo += 1;
            }
            else
            {
                posInfo.ZCounterNo += 1;
            }

            var recentInvoice = GetOrderNumber(orders.Max(o => o?.InvoiceNumber));

            var isSameLastInvoice = await _dataContext.Reading
                .OrderBy(r => r.Id)
                .LastOrDefaultAsync(l => l.LastInvoice == recentInvoice);

            // Determine reading values based on whether the invoice already exists
            var reading = new Reading
            {
                IsTrainMode = isTrainMode,
                LastInvoice = recentInvoice,
                Present = presentAccumulatedSales,
                Previous = isSameLastInvoice != null ? presentAccumulatedSales : previousAccumulatedSales,
                Sales = isSameLastInvoice != null ? 0m : salesForTheDay,
            };
            await _dataContext.Reading.AddAsync(reading);
            await _dataContext.SaveChangesAsync();

            return dto;
        }


        public async Task<List<GetInvoiceDocumentDTO>> InvoiceDocuments(DateTime fromDate, DateTime toDate)
        {
            return await _dataContext.InvoiceDocument
                .Include(d => d.Invoice)
                .Include(d => d.Manager)
                .Where(d => d.CreatedAt.Date >= fromDate.Date && d.CreatedAt.Date <= toDate.Date)
                .Select(d => new GetInvoiceDocumentDTO
                {
                    Id = d.Id,
                    Type = d.Type,
                    TypeDisplay = d.Type == InvoiceDocumentType.Invoice ? d.Type + $" #{d.Invoice!.InvoiceNumber}" : d.Type,
                    Status = d.Type == InvoiceDocumentType.Invoice ? d.Invoice!.Status : "",
                    CreatedAt = d.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd hh:mm:ss")
                })
                .ToListAsync();
        }

        private string GetOrderNumber(long? orderId)
        {
            return orderId.HasValue ? orderId.Value.ToString("D12") : 0.ToString("D12");
        }


        private async Task<(List<TransactionListDTO>, TotalTransactionListDTO)> GetTransactListData(DateTime fromDate, DateTime toDate)
        {
            // Set start date to beginning of day and end date to end of day
            var startDate = fromDate.Date;
            var endDate = toDate.Date.AddDays(1).AddTicks(-1);

            var isTrainMode = await _terminalMachine.IsTrainMode();

            // Get all orders with necessary includes
            var orders = _dataContext.Invoice
                .Include(o => o.Items)
                .Include(o => o.Cashier)
                .Where(o => o.IsTrainMode == isTrainMode)
                .AsEnumerable()
                .Where(o => o.CreatedAt.Date >= startDate.Date && o.CreatedAt.Date <= endDate.Date)
                .OrderBy(o => o.InvoiceNumber)
                .ToList();

            var transactionList = new List<TransactionListDTO>();
            var totalTransactionList = new TotalTransactionListDTO();

            foreach (var order in orders)
            {
                // Calculate base amounts and round to 2 decimal places
                var subTotal = Math.Round(order.SubTotal ?? 0m, 2);
                var amountDue = Math.Round(order.DueAmount ?? 0m, 2);
                var grossSales = Math.Round(order.GrossAmount, 2);
                var returns = Math.Round(order.ReturnedAmount ?? 0m, 2);
                var lessDiscount = Math.Round(order.DiscountAmount ?? 0m, 2);
                var netOfSales = Math.Round(subTotal - lessDiscount - returns, 2);

                // Calculate VAT amounts proportionally based on refunded amount
                var refundRatio = order.TotalAmount > 0 ? (order.ReturnedAmount ?? 0m) / order.TotalAmount : 0m;
                var vatable = Math.Round((order.VatSales ?? 0m) * (1 - refundRatio), 2);
                var zeroRated = Math.Round(order.VatZero ?? 0m, 2);
                var exempt = Math.Round((order.VatExempt ?? 0m) * (1 - refundRatio), 2);
                var vat = Math.Round((order.VatAmount ?? 0m) * (1 - refundRatio), 2);

                var discType = !string.IsNullOrWhiteSpace(order.DiscountType)
                    ? (order.DiscountType.StartsWith("s-") ? order.DiscountType.Substring(2) : order.DiscountType)
                    : "";

                // Create initial transaction entry
                var baseTransaction = new TransactionListDTO
                {
                    Date = order.CreatedAt.ToString("MM/dd/yyyy"),
                    InvoiceNum = order.InvoiceNumber.ToString("D12"),
                    Src = "",
                    DiscType = discType,
                    Percent = order.DiscountPercent?.ToString() ?? "",
                    SubTotal = subTotal,
                    AmountDue = amountDue,
                    GrossSales = grossSales,
                    Returns = 0m,
                    NetOfReturns = Math.Round(grossSales - returns, 2),
                    LessDiscount = lessDiscount,
                    NetOfSales = netOfSales,
                    Vatable = vatable,
                    ZeroRated = zeroRated,
                    Exempt = exempt,
                    Vat = vat
                };

                // Add the initial transaction
                transactionList.Add(baseTransaction);

                // Update totals for base transaction
                UpdateTotals(totalTransactionList, grossSales, returns, lessDiscount, netOfSales, vatable, exempt, vat);

                // If order was cancelled, add a cancellation entry
                if (order.Status == InvoiceStatusType.Void)
                {
                    var cancelledTransaction = new TransactionListDTO
                    {
                        Date = order.StatusChangeDate.Value.ToString("MM/dd/yyyy"),
                        InvoiceNum = $"{order.InvoiceNumber:D12}",
                        Src = "VOIDED",
                        DiscType = discType,
                        Percent = order.DiscountPercent?.ToString() ?? "",
                        SubTotal = Math.Round(-subTotal, 2),
                        AmountDue = Math.Round(-amountDue, 2),
                        GrossSales = Math.Round(-grossSales, 2),
                        Returns = 0m,
                        NetOfReturns = Math.Round(-grossSales, 2),
                        LessDiscount = Math.Round(-lessDiscount, 2),
                        NetOfSales = Math.Round(-netOfSales, 2),
                        Vatable = Math.Round(-vatable, 2),
                        ZeroRated = 0m,
                        Exempt = Math.Round(-exempt, 2),
                        Vat = Math.Round(-vat, 2)
                    };
                    transactionList.Add(cancelledTransaction);

                    // Update totals for cancellation
                    UpdateTotals(totalTransactionList, -grossSales, 0m, -lessDiscount, -netOfSales, -vatable, -exempt, -vat);
                }

                // If order has refunds, add a return entry
                if (order.Status == InvoiceStatusType.Returned && returns > 0)
                {
                    var returnedTransaction = new TransactionListDTO
                    {
                        Date = order.StatusChangeDate.Value.ToString("MM/dd/yyyy"),
                        InvoiceNum = $"{order.InvoiceNumber:D12}",
                        Src = "REFUNDED",
                        DiscType = discType,
                        Percent = order.DiscountPercent?.ToString() ?? "",
                        SubTotal = Math.Round(-returns, 2),
                        AmountDue = 0m,
                        GrossSales = -returns,
                        Returns = returns,
                        NetOfReturns = -returns,
                        LessDiscount = 0m, // No discount on refunds
                        NetOfSales = -returns,
                        Vatable = Math.Round(-vatable * (returns / grossSales), 2),
                        ZeroRated = 0m,
                        Exempt = Math.Round(-exempt * (returns / grossSales), 2),
                        Vat = Math.Round(-vat * (returns / grossSales), 2)
                    };
                    transactionList.Add(returnedTransaction);

                    // Update totals for return
                    UpdateTotals(totalTransactionList, -returns, returns, 0m, -returns,
                        -vatable * (returns / grossSales),
                        -exempt * (returns / grossSales),
                        -vat * (returns / grossSales));
                }
            }

            return (transactionList.OrderBy(t => t.InvoiceNum).ToList(), totalTransactionList);
        }

        private void UpdateTotals(TotalTransactionListDTO totals, decimal grossSales, decimal returns,
            decimal lessDiscount, decimal netOfSales, decimal vatable, decimal exempt, decimal vat)
        {
            totals.TotalGrossSales = Math.Round(totals.TotalGrossSales + grossSales, 2);
            totals.TotalReturns = Math.Round(totals.TotalReturns + returns, 2);
            totals.TotalNetOfReturns = Math.Round(totals.TotalNetOfReturns + (grossSales - returns), 2);
            totals.TotalLessDiscount = Math.Round(totals.TotalLessDiscount + lessDiscount, 2);
            totals.TotalNetOfSales = Math.Round(totals.TotalNetOfSales + netOfSales, 2);
            totals.TotalVatable = Math.Round(totals.TotalVatable + vatable, 2);
            totals.TotalExempt = Math.Round(totals.TotalExempt + exempt, 2);
            totals.TotalVat = Math.Round(totals.TotalVat + vat, 2);
        }

        public async Task<(List<TransactionListDTO> Data, TotalTransactionListDTO Totals, string FilePath)> GetTransactList(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var posInfo = await _terminalMachine.GetTerminalInfo();

                _pDFService.UpdateBusinessInfo(
                    posInfo.RegisteredName ?? "N/A",
                    posInfo.Address ?? "N/A",
                    posInfo.VatTinNumber ?? "N/A"
                );

                // Get the transaction list data
                var (transactions, totals) = await GetTransactListData(fromDate, toDate);

                // Generate PDF
                var pdfBytes = _pDFService.GenerateTransactionListPDF(transactions, fromDate, toDate);


                // BASE name (no suffix):
                var baseName = $"TranxList_{fromDate:yyyyMMdd}_to_{toDate:yyyyMMdd}";

                // Append a timestamp so it's always unique
                var uniqueSuffix = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                var fileName = $"{baseName}_{uniqueSuffix}.pdf";
                var folderPath = FolderPath.PDF.TransactionLists;

                // Ensure directory exists
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, fileName);
                // Save PDF file
                await File.WriteAllBytesAsync(filePath, pdfBytes);

                // Save PDF file
                await File.WriteAllBytesAsync(filePath, pdfBytes);

                return (transactions, totals, filePath);
            }
            catch (Exception ex)
            {
                // Log the error appropriately
                throw new Exception($"Error generating transaction list report: {ex.Message}", ex);
            }
        }
        private async Task<List<AuditTrailDTO>> GetAuditTrailData(DateTime fromDate, DateTime toDate)
        {
            var auditTrail = new List<AuditTrailDTO>();
            var startDate = fromDate.Date;
            var endDate = toDate.Date.AddDays(1);
            const string TIME_FORMAT = "hh:mm tt";

            // Get user logs
            var userLogs = await _dataContext.AuditLog
                .Include(m => m.Cashier)
                .Include(m => m.Manager)
                .Where(c => c.CreatedAt >= startDate && c.CreatedAt < endDate)
                .Select(m => new AuditTrailDTO
                {
                    Date = m.CreatedAt.DateFormat(),
                    Time = m.CreatedAt.ToString(TIME_FORMAT),
                    UserName = m.Manager != null
                        ? $"{m.Manager.FName} {m.Manager.LName}"
                        : $"{m.Cashier.FName} {m.Cashier.LName}",
                    Action = m.Action,
                    Amount = m.Amount > 0
                        ? m.Amount.PesoFormat()
                        : null,
                    SortDateTime = m.CreatedAt.ToLocalTime()
                })
                .ToListAsync();

            auditTrail.AddRange(userLogs);

            // Get timestamps
            var timestamps = await _dataContext.Timestamp
                .Include(t => t.Cashier)
                .Include(t => t.ManagerIn)
                .Include(t => t.ManagerOut)
                .ToListAsync();

            // Filter timestamps in memory
            var filteredTimestamps = timestamps.Where(t =>
                (t.TsIn.HasValue && t.TsIn.Value.Date >= startDate && t.TsIn.Value.Date < endDate) ||
                (t.TsOut.HasValue && t.TsOut.Value.Date >= startDate && t.TsOut.Value.Date < endDate))
                .ToList();

            foreach (var t in filteredTimestamps)
            {
                // Process TsIn events
                if (t.TsIn.HasValue)
                {
                    var tsInDateTime = t.TsIn.Value;
                    var action = t.CashInDrawerAmount.HasValue ? "Set Cash in Drawer" : "Log In";
                    var amount = t.CashInDrawerAmount.HasValue
                        ? t.CashInDrawerAmount.Value.PesoFormat()
                        : null;

                    auditTrail.Add(new AuditTrailDTO
                    {
                        Date = tsInDateTime.DateFormat(),
                        Time = tsInDateTime.ToString(TIME_FORMAT),
                        UserName = t.ManagerIn != null
                            ? $"{t.ManagerIn.FName} {t.ManagerIn.LName}"
                            : "Super Admin",
                        Action = action,
                        Amount = amount,
                        SortDateTime = tsInDateTime
                    });
                }

                // Process TsOut events
                if (t.TsOut.HasValue)
                {
                    var tsOutDateTime = t.TsOut.Value;
                    var manager = t.ManagerOut ?? t.ManagerIn;
                    var action = t.CashOutDrawerAmount.HasValue ? "Set Cash out Drawer" : "Log Out";
                    var amount = t.CashOutDrawerAmount.HasValue
                        ? t.CashOutDrawerAmount.Value.PesoFormat()
                        : null;

                    auditTrail.Add(new AuditTrailDTO
                    {
                        Date = tsOutDateTime.DateFormat(),
                        Time = tsOutDateTime.ToString(TIME_FORMAT),
                        UserName = manager != null
                            ? $"{manager.FName} {manager.LName}"
                            : "Super Admin",
                        Action = action,
                        Amount = amount,
                        SortDateTime = tsOutDateTime
                    });
                }
            }

            return auditTrail.OrderBy(a => a.SortDateTime).ToList();
        }

        public async Task<string> GetAuditTrail(DateTime fromDate, DateTime toDate)
        {
            try
            {

                var posInfo = await _terminalMachine.GetTerminalInfo();

                // Update PDF services with business info
                _auditTrailPDFService.UpdateBusinessInfo(
                    posInfo.RegisteredName ?? "N/A",
                    posInfo.Address ?? "N/A",
                    posInfo.VatTinNumber ?? "N/A",
                    posInfo.MinNumber ?? "N/A",
                    posInfo.PosSerialNumber ?? "N/A"
                );
                // Get the audit trail data
                var auditTrail = await GetAuditTrailData(fromDate, toDate);

                // Generate PDF
                var pdfBytes = _auditTrailPDFService.GenerateAuditTrailPDF(auditTrail, fromDate, toDate);

                // BASE name (no suffix):
                var baseName = $"AuditTrail_{fromDate:yyyyMMdd}_to_{toDate:yyyyMMdd}";

                // Append a timestamp so it's always unique
                var uniqueSuffix = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                var fileName = $"{baseName}_{uniqueSuffix}.pdf";
                var folderPath = FolderPath.PDF.AuditTrail;

                // Ensure directory exists
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, fileName);
                // Save PDF file
                await File.WriteAllBytesAsync(filePath, pdfBytes);

                return (filePath);
            }
            catch (Exception ex)
            {
                // Log the error appropriately
                throw new Exception($"Error generating audit trail report: {ex.Message}", ex);
            }
        }

        private async Task<List<SalesReportDTO>> GetSalesReportData(DateTime fromDate, DateTime toDate)
        {
            // Convert DateTime to DateTimeOffset for proper comparison
            var startDate = new DateTimeOffset(fromDate.Date);
            var endDate = new DateTimeOffset(toDate.Date.AddDays(1).AddTicks(-1));

            var isTrainMode = await _terminalMachine.IsTrainMode();

            // Get all orders with necessary includes and switch to client evaluation
            var invoices = await _dataContext.Invoice
                .Where(o => o.Status != InvoiceStatusType.Pending)
                .Include(o => o.Items)
                .ThenInclude(m => m.Product)
                .ThenInclude(m => m.Category)
                .ToListAsync(); // First get all data from database

            // Then filter and sort in memory
            var filteredOrders = invoices
                .Where(o => o.IsTrainMode == isTrainMode &&
                           o.CreatedAt.Date >= startDate.Date &&
                           o.CreatedAt.Date <= endDate.Date)
                .OrderBy(o => o.InvoiceNumber)
                .ToList();

            var salesReport = new List<SalesReportDTO>();

            foreach (var order in filteredOrders)
            {
                // Skip cancelled orders entirely
                if (order.Status == InvoiceStatusType.Void) continue;

                foreach (var item in order.Items.Where(i => i.Status != InvoiceStatusType.Void)) // Exclude voided items
                {

                    // Calculate the return amount for this specific item if it was refunded
                    decimal itemTotal = item.SubTotal;
                    decimal returnAmount = 0m;
                    if (item.Status == InvoiceStatusType.Returned && order.ReturnedAmount.HasValue && order.TotalAmount > 0)
                    {
                        // Proportional calculation based on the item's contribution to the total order
                        decimal returnRatio = itemTotal / order.TotalAmount;
                        returnAmount = order.ReturnedAmount.Value * returnRatio;
                    }

                    // Create a single report entry for the item
                    salesReport.Add(new SalesReportDTO
                    {
                        InvoiceDate = item.Status == InvoiceStatusType.Returned && order.StatusChangeDate.HasValue ?
                            order.StatusChangeDate.Value :
                            order.CreatedAt, // Use return date if refunded
                        InvoiceNumber = order.InvoiceNumber,
                        MenuName = item.Product.Name,
                        BaseUnit = item.Product.BaseUnit ?? "",
                        Quantity = item.Qty,
                        Cost = item.Product.Cost, // Cost remains the same whether sold or returned
                        Price = item.Price,
                        ItemGroup = item.Product.Category?.CtgryName ?? "",
                        Barcode = item.Product.Barcode,
                        IsReturned = item.Status == InvoiceStatusType.Returned, // Use item's IsRefund flag
                        ReturnDate = item.Status == InvoiceStatusType.Returned ? order.StatusChangeDate : null, // Include return date only if refunded
                        ReturnAmount = returnAmount // Include calculated return amount
                    });
                }
            }

            return salesReport;
        }
        public async Task<string> GetSalesReport(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var posInfo = await _terminalMachine.GetTerminalInfo();

                _salesReportPDFService.UpdateBusinessInfo(
                    posInfo.RegisteredName ?? "N/A",
                    posInfo.Address ?? "N/A",
                    posInfo.VatTinNumber ?? "N/A"
                );
                // Get the sales report data
                var salesData = await GetSalesReportData(fromDate, toDate);

                // Generate PDF
                var pdfBytes = _salesReportPDFService.GenerateSalesReportPDF(salesData, fromDate, toDate);

                // BASE name (no suffix):
                var baseName = $"SalesReport_{fromDate:yyyyMMdd}_to_{toDate:yyyyMMdd}";

                // Append a timestamp so it's always unique
                var uniqueSuffix = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                var fileName = $"{baseName}_{uniqueSuffix}.pdf";
                var folderPath = FolderPath.PDF.SalesHistory;

                // Ensure directory exists
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, fileName);
                // Save PDF file
                await File.WriteAllBytesAsync(filePath, pdfBytes);

                return (filePath);
            }
            catch (Exception ex)
            {
                // Log the error appropriately
                throw new Exception($"Error generating sales report: {ex.Message}", ex);
            }
        }
        public async Task<string> GetSalesBook(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var posInfo = await _terminalMachine.GetTerminalInfo();

                _salesReportPDFService.UpdateBusinessInfo(
                    posInfo.RegisteredName ?? "N/A",
                    posInfo.Address ?? "N/A",
                    posInfo.VatTinNumber ?? "N/A"
                );
                // Get the sales report data
                var salesBook = await _dataContext.Reading
                    .Where(r => r.IsTrainMode == posInfo.IsTrainMode &&
                        r.CreatedAt.Date >= fromDate.Date && r.CreatedAt.Date <= toDate.Date)
                    .ToListAsync();

                // Generate PDF
                var pdfBytes = _salesReportPDFService.GenerateSalesBookPDF(salesBook, fromDate, toDate);

                // BASE name (no suffix):
                var baseName = $"SalesBook_{fromDate:yyyyMMdd}_to_{toDate:yyyyMMdd}";

                // Append a timestamp so it's always unique
                var uniqueSuffix = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                var fileName = $"{baseName}_{uniqueSuffix}.pdf";
                var folderPath = FolderPath.PDF.SalesBook;

                // Ensure directory exists
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, fileName);
                // Save PDF file
                await File.WriteAllBytesAsync(filePath, pdfBytes);

                return (filePath);
            }
            catch (Exception ex)
            {
                // Log the error appropriately
                throw new Exception($"Error generating sales report: {ex.Message}", ex);
            }
        }

        private async Task<(List<VoidedListDTO> voidedOrdersLists, TotalVoidedListDTO totalVoidedList)> GetVoidedListsData(DateTime fromDate, DateTime toDate)
        {
            // Set start date to beginning of day and end date to end of day
            var startDate = fromDate.Date;
            var endDate = toDate.Date.AddDays(1).AddTicks(-1);
            var isTrainMode = await _terminalMachine.IsTrainMode();

            // Get all voided orders with necessary includes
            var voidedOrders = await _dataContext.Invoice
                .Include(o => o.Items)
                    .ThenInclude(p => p.Product)
                .Include(o => o.Cashier)
                .Include(o => o.VoidedBy)
                .Where(o => o.Status == InvoiceStatusType.Void && o.IsTrainMode == isTrainMode)
                .Where(o => o.CreatedAt.Date >= startDate.Date && o.CreatedAt.Date <= endDate.Date)
                .OrderBy(o => o.InvoiceNumber)
                .ToListAsync();

            var voidedList = new List<VoidedListDTO>();
            foreach (var order in voidedOrders)
            {
                var totalAmount = order.TotalAmount;

                var voidedItemsList = new List<VoidedItemListDTO>();
                var voidedItems = order.Items.Where(i => i.Status == InvoiceStatusType.Void).ToList();

                var index = 0;
                foreach (var item in voidedItems)
                {
                    index++;
                    voidedItemsList.Add(new VoidedItemListDTO
                    {
                        No = index,
                        Barcode = item.Product?.Barcode ?? string.Empty,
                        ItemName = item.Product?.Name ?? string.Empty,
                        Quantity = item.Qty,
                        Price = item.Price,
                        Amount = item.SubTotal,
                        Return = 0,
                        Reason = order.Reason ?? "No Reason provided.",
                        ReturnQuantity = 0,
                    });
                }


                // Create a new VoidedListDTO entry
                var voidedEntry = new VoidedListDTO
                {
                    Date = order.StatusChangeDate != null ?
                        order.StatusChangeDate.Value.DateFormat() :
                        order.CreatedAt.DateFormat(),
                    InvoiceNum = order.InvoiceNumber.InvoiceFormat(),
                    DiscType = order.DiscountType ?? "N/A",
                    Percent = order.DiscountPercent?.ToString() ?? "0",
                    GrossSales = order.GrossAmount,
                    Discount = order.DiscountAmount ?? 0m,
                    AmountDue = order.DueAmount ?? 0m,
                    Vatable = order.VatSales ?? 0m,
                    ZeroRated = order.VatZero ?? 0m,
                    Exempt = order.VatExempt ?? 0m,
                    Vat = order.VatAmount ?? 0m,
                    Reason = order.Reason ?? "No Reason provided.",
                    User = order.Cashier.FullName,
                    CancelledBy = order.VoidedBy?.FullName ?? "N/A",
                    CancelledDate = order.StatusChangeDate != null ?
                        order.StatusChangeDate.Value.DateFormat() :
                        order.CreatedAt.DateFormat(),
                    CancelledTime = order.StatusChangeDate != null ?
                        order.StatusChangeDate.Value.ToString("hh:mm tt") :
                        order.CreatedAt.ToString("hh:mm tt"),
                    VoidedItemList = voidedItemsList,

                };
                voidedList.Add(voidedEntry);



            }
            var totalVoided = new TotalVoidedListDTO
            {
                TotalGross = voidedOrders.Sum(g => g.GrossAmount),
                TotalDiscount = voidedOrders.Sum(d => d.DiscountAmount ?? 0m),
                TotalAmountDue = voidedOrders.Sum(a => a.DueAmount ?? 0m),
                TotalVatable = voidedOrders.Sum(v => v.VatSales ?? 0m),
                TotalVatZero = voidedOrders.Sum(z => z.VatZero ?? 0m),
                TotalExempt = voidedOrders.Sum(e => e.VatExempt ?? 0m),
                TotalVat = voidedOrders.Sum(v => v.VatAmount ?? 0m),

            };
            return (voidedList, totalVoided);
        }

        public async Task<string> GetVoidedListsReport(DateTime fromDate, DateTime toDate)
        {
            try
            {
                // Get the sales report data
                var voidData = await GetVoidedListsData(fromDate, toDate);

                // Generate PDF
                var pdfBytes = _voidedListPDFService.GenerateVoidedListPDF(voidData.voidedOrdersLists, voidData.totalVoidedList, fromDate, toDate);

                // BASE name (no suffix):
                var baseName = $"VoidedLists_{fromDate:yyyyMMdd}_to_{toDate:yyyyMMdd}";

                // Append a timestamp so it's always unique
                var uniqueSuffix = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                var fileName = $"{baseName}_{uniqueSuffix}.pdf";
                var folderPath = FolderPath.PDF.VoidedLists;

                // Ensure directory exists
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, fileName);
                // Save PDF file
                await File.WriteAllBytesAsync(filePath, await pdfBytes);

                return (filePath);
            }
            catch (Exception ex)
            {
                // Log the error appropriately
                throw new Exception($"Error generating sales report: {ex.Message}", ex);
            }
        }

        private async Task<(List<TransactionListDTO>, TotalTransactionListDTO)> GetPwdOrSeniorData(DateTime fromDate, DateTime toDate, string type)
        {
            // Set start date to beginning of day and end date to end of day
            var startDate = fromDate.Date;
            var endDate = toDate.Date.AddDays(1).AddTicks(-1);

            var isTrainMode = await _terminalMachine.IsTrainMode();

            // Get all orders with necessary includes
            var orders = _dataContext.Invoice
                .Include(o => o.Items)
                .Include(o => o.Cashier)
                .Where(o => o.IsTrainMode == isTrainMode)
                .AsEnumerable()
                .Where(o => o.CreatedAt.Date >= startDate.Date
                    && o.CreatedAt.Date <= endDate.Date
                    && o.DiscountType == type)
                .OrderBy(o => o.InvoiceNumber)
                .ToList();

            var transactionList = new List<TransactionListDTO>();
            var totalTransactionList = new TotalTransactionListDTO();

            foreach (var order in orders)
            {
                // Calculate base amounts and round to 2 decimal places
                var subTotal = Math.Round(order.SubTotal ?? 0m, 2);
                var amountDue = Math.Round(order.DueAmount ?? 0m, 2);
                var grossSales = Math.Round(order.GrossAmount, 2);
                var returns = Math.Round(order.ReturnedAmount ?? 0m, 2);
                var lessDiscount = Math.Round(order.DiscountAmount ?? 0m, 2);
                var netOfSales = Math.Round(subTotal - lessDiscount - returns, 2);

                // Calculate VAT amounts proportionally based on refunded amount
                var refundRatio = order.TotalAmount > 0 ? (order.ReturnedAmount ?? 0m) / order.TotalAmount : 0m;
                var vatable = Math.Round((order.VatSales ?? 0m) * (1 - refundRatio), 2);
                var zeroRated = Math.Round(order.VatZero ?? 0m, 2);
                var exempt = Math.Round((order.VatExempt ?? 0m) * (1 - refundRatio), 2);
                var vat = Math.Round((order.VatAmount ?? 0m) * (1 - refundRatio), 2);

                var discType = !string.IsNullOrWhiteSpace(order.DiscountType)
                    ? (order.DiscountType.StartsWith("s-") ? order.DiscountType.Substring(2) : order.DiscountType)
                    : "";

                // Create initial transaction entry
                var baseTransaction = new TransactionListDTO
                {
                    Date = order.CreatedAt.ToString("MM/dd/yyyy"),
                    InvoiceNum = order.InvoiceNumber.ToString("D12"),
                    Src = "",
                    DiscType = discType,
                    Percent = order.DiscountPercent?.ToString() ?? "",
                    SubTotal = subTotal,
                    AmountDue = amountDue,
                    GrossSales = grossSales,
                    Returns = 0m,
                    NetOfReturns = Math.Round(grossSales - returns, 2),
                    LessDiscount = lessDiscount,
                    NetOfSales = netOfSales,
                    Vatable = vatable,
                    ZeroRated = zeroRated,
                    Exempt = exempt,
                    Vat = vat
                };

                // Add the initial transaction
                transactionList.Add(baseTransaction);

                // Update totals for base transaction
                UpdateTotals(totalTransactionList, grossSales, returns, lessDiscount, netOfSales, vatable, exempt, vat);

                // If order was cancelled, add a cancellation entry
                if (order.Status == InvoiceStatusType.Void)
                {
                    var cancelledTransaction = new TransactionListDTO
                    {
                        Date = order.StatusChangeDate.Value.ToString("MM/dd/yyyy"),
                        InvoiceNum = $"{order.InvoiceNumber:D12}",
                        Src = "VOIDED",
                        DiscType = discType,
                        Percent = order.DiscountPercent?.ToString() ?? "",
                        SubTotal = Math.Round(-subTotal, 2),
                        AmountDue = Math.Round(-amountDue, 2),
                        GrossSales = Math.Round(-grossSales, 2),
                        Returns = 0m,
                        NetOfReturns = Math.Round(-grossSales, 2),
                        LessDiscount = Math.Round(-lessDiscount, 2),
                        NetOfSales = Math.Round(-netOfSales, 2),
                        Vatable = Math.Round(-vatable, 2),
                        ZeroRated = 0m,
                        Exempt = Math.Round(-exempt, 2),
                        Vat = Math.Round(-vat, 2)
                    };
                    transactionList.Add(cancelledTransaction);

                    // Update totals for cancellation
                    UpdateTotals(totalTransactionList, -grossSales, 0m, -lessDiscount, -netOfSales, -vatable, -exempt, -vat);
                }

                // If order has refunds, add a return entry
                if (order.Status == InvoiceStatusType.Returned && returns > 0)
                {
                    var returnedTransaction = new TransactionListDTO
                    {
                        Date = order.StatusChangeDate.Value.ToString("MM/dd/yyyy"),
                        InvoiceNum = $"{order.InvoiceNumber:D12}",
                        Src = "REFUNDED",
                        DiscType = discType,
                        Percent = order.DiscountPercent?.ToString() ?? "",
                        SubTotal = Math.Round(-returns, 2),
                        AmountDue = 0m,
                        GrossSales = -returns,
                        Returns = returns,
                        NetOfReturns = -returns,
                        LessDiscount = 0m, // No discount on refunds
                        NetOfSales = -returns,
                        Vatable = Math.Round(-vatable * (returns / grossSales), 2),
                        ZeroRated = 0m,
                        Exempt = Math.Round(-exempt * (returns / grossSales), 2),
                        Vat = Math.Round(-vat * (returns / grossSales), 2)
                    };
                    transactionList.Add(returnedTransaction);

                    // Update totals for return
                    UpdateTotals(totalTransactionList, -returns, returns, 0m, -returns,
                        -vatable * (returns / grossSales),
                        -exempt * (returns / grossSales),
                        -vat * (returns / grossSales));
                }
            }

            return (transactionList.OrderBy(t => t.InvoiceNum).ToList(), totalTransactionList);
        }
        public async Task<(List<TransactionListDTO> Data, TotalTransactionListDTO Totals, string FilePath)> GetPwdOrSeniorList(DateTime fromDate, DateTime toDate, string type)
        {
            try
            {
                var posInfo = await _terminalMachine.GetTerminalInfo();

                _pDFService.UpdateBusinessInfo(
                    posInfo.RegisteredName ?? "N/A",
                    posInfo.Address ?? "N/A",
                    posInfo.VatTinNumber ?? "N/A"
                );

                // Get the transaction list data
                var (transactions, totals) = await GetPwdOrSeniorData(fromDate, toDate, type);

                // Generate PDF
                var pdfBytes = _pDFService.GeneratePwdOrSeniorListPDF(transactions, fromDate, toDate, type);

                var typeName = type == DiscountType.SeniorCitizen ? "SeniorCitizen" : "PWD";

                // BASE name (no suffix):
                var baseName = $"{typeName}_{fromDate:yyyyMMdd}_to_{toDate:yyyyMMdd}";

                // Append a timestamp so it's always unique
                var uniqueSuffix = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                var fileName = $"{baseName}_{uniqueSuffix}.pdf";
                var folderPath = FolderPath.PDF.GetPath(type);

                // Ensure directory exists
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, fileName);
                // Save PDF file
                await File.WriteAllBytesAsync(filePath, pdfBytes);

                // Save PDF file
                await File.WriteAllBytesAsync(filePath, pdfBytes);

                return (transactions, totals, filePath);
            }
            catch (Exception ex)
            {
                // Log the error appropriately
                throw new Exception($"Error generating transaction list report: {ex.Message}", ex);
            }
        }

    }
}
