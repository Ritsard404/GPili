﻿using Microsoft.EntityFrameworkCore;
using ServiceLibrary.Data;
using ServiceLibrary.Models;
using ServiceLibrary.Services.Interfaces;
using ServiceLibrary.Utils;
using System.Diagnostics;
using static ServiceLibrary.Utils.FolderPath;

namespace ServiceLibrary.Services.Repositories
{
    public class AuditLogRepository(DataContext _dataContext) : IAuditLog
    {
        private static readonly HttpClient _httpClient = new()
        {
            BaseAddress = new Uri(JournalLink.Ebisx)
        };

        private async Task<bool> IsTrainMode()
        {
            return await _dataContext.PosTerminalInfo.Select(t => t.IsTrainMode).FirstOrDefaultAsync();
        }
        private async Task<PosTerminalInfo?> GetTerminalInfo()
        {
            return await _dataContext.PosTerminalInfo
                .AsNoTracking()
                .SingleOrDefaultAsync();
        }
        public async Task<(bool isSuccess, string message)> AddCashierAudit(User cashier, string action, string changes, decimal? amount)
        {
            var isTrainMode = await IsTrainMode();

            _dataContext.AuditLog.Add(new AuditLog
            {
                CashierEmail = cashier.Email,
                Action = action,
                Changes = changes,
                Amount = amount,
                isTrainMode = isTrainMode
            });

            await _dataContext.SaveChangesAsync();
            return (true, "Cashier audit added successfully.");
        }

        public async Task<(bool isSuccess, string message)> AddItemsJournal(long invId)
        {
            var terminalinfo = await GetTerminalInfo();
            if (terminalinfo == null)
                return (false, "Terminal information not found.");

            var invoice = await _dataContext.Invoice
                .Include(i => i.Cashier)
                .Include(i => i.Items)
                    .ThenInclude(i => i.Product)
                .Include(i => i.Cashier)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == invId);

            if (invoice == null)
                return (false, "Invoice not found.");

            if (invoice.IsTrainMode)
                return (false, "Cannot add journal entries in training mode.");

            if (invoice.Items == null || !invoice.Items.Any())
                return (false, "No items found in the invoice.");

            var itemJournals = new List<Journal>();

            var status = invoice.Status == InvoiceStatusType.Paid ? "Posted" :
                invoice.Status == InvoiceStatusType.Returned ? "Returned" : "Unposted";

            foreach (var item in invoice.Items)
            {

                itemJournals.Add(new Journal
                {
                    Entry_No = invoice.InvoiceNumber.InvoiceFormat(),
                    Entry_Line_No = 3.ToString(),
                    Entry_Date = invoice.CreatedAt.DateFormat(),
                    CostCenter = terminalinfo.CostCenter,
                    ItemId = item.Product.ProdId,
                    Unit = item.Product.BaseUnit,
                    Qty = item.Qty.ToString(),
                    Cost = item.Product.Cost.ToString(),
                    Price = item.Price.ToString(),
                    TotalPrice = item.SubTotal.ToString(),
                    Debit = item.SubTotal.ToString(),
                    Credit = "0.00",
                    AccountBalance = "",
                    Prev_Reading = "",
                    Curr_Reading = "",
                    Memo = "Item Sale",
                    AccountName = item.Product.Name,
                    Reference = invoice.InvoiceNumber.InvoiceFormat(),
                    Entry_Name = "1",
                    Cashier = invoice.Cashier.Email,
                    Count_Type = "",
                    Deposited = "0.00",
                    Deposit_Date = "",
                    Deposit_Reference = "",
                    Deposit_By = "",
                    Deposit_Time = "",
                    CustomerName = invoice.CustomerName,
                    SubTotal = "",
                    TotalTax = "",
                    GrossTotal = "",
                    Discount_Type = "",
                    Discount_Amount = "",
                    NetPayable = "",
                    Status = status,
                    User_Email = invoice.Cashier.Email,
                    QtyPerBaseUnit = "1",
                    QtyBalanceInBaseUnit = "0",
                    IsPushed = false
                });
            }

            _dataContext.AccountJournal.AddRange(itemJournals);
            await _dataContext.SaveChangesAsync();

            return (true, "Journal entries successfully added.");
        }

        public async Task<(bool isSuccess, string message)> AddManagerAudit(User manager, string action, string changes, decimal? amount)
        {
            var isTrainMode = await IsTrainMode();

            _dataContext.AuditLog.Add(new AuditLog
            {
                ManagerEmail = manager.Email,
                Action = action,
                Changes = changes,
                Amount = amount,
                isTrainMode = isTrainMode
            });

            await _dataContext.SaveChangesAsync();
            return (true, "Manager audit added successfully.");
        }

        public async Task<(bool isSuccess, string message)> AddPwdScJournal(long invId)
        {
            var terminalinfo = await GetTerminalInfo();
            if (terminalinfo == null)
                return (false, "Terminal information not found.");

            var invoice = await _dataContext.Invoice
                .Include(i => i.Cashier)
                .FirstOrDefaultAsync(i => i.Id == invId);

            if (invoice == null)
                return (false, "Invoice not found.");

            if (invoice.IsTrainMode)
                return (false, "Cannot add journal entries in training mode.");

            if (string.IsNullOrEmpty(invoice.EligibleDiscName))
                return (false, "No PWD/SC information found in the invoice.");

            var status = invoice.Status == InvoiceStatusType.Paid ? "Posted" :
                invoice.Status == InvoiceStatusType.Returned ? "Returned" : "Unposted";

            var journal = new Journal
            {
                Entry_No = invoice.InvoiceNumber.InvoiceFormat(),
                Entry_Line_No = 5.ToString(),
                Entry_Date = invoice.CreatedAt.DateFormat(),
                CostCenter = terminalinfo.CostCenter,
                ItemId = "N/A",
                Unit = "N/A",
                Qty = "0",
                Cost = "0.00",
                Price = "",
                TotalPrice = "0",
                Debit = "0",
                Credit = "0.00",
                AccountBalance = "",
                Prev_Reading = "",
                Curr_Reading = "",
                Memo = "PWD/SC Discount",
                AccountName = invoice.EligibleDiscName,
                Reference = invoice.InvoiceNumber.InvoiceFormat(),
                Entry_Name = "1",
                Cashier = invoice.Cashier.Email,
                Count_Type = "",
                Deposited = "0.00",
                Deposit_Date = "",
                Deposit_Reference = "",
                Deposit_By = "",
                Deposit_Time = "",
                CustomerName = invoice.CustomerName,
                SubTotal = "",
                TotalTax = "",
                GrossTotal = "",
                Discount_Type = "",
                Discount_Amount = "",
                NetPayable = "",
                Status = invoice.Status,
                User_Email = invoice.Cashier.Email,
                QtyPerBaseUnit = "1",
                QtyBalanceInBaseUnit = "0",
                IsPushed = false
            };

            _dataContext.AccountJournal.Add(journal);
            await _dataContext.SaveChangesAsync();

            return (true, "PWD/SC journal entry successfully added.");
        }

        public async Task<(bool isSuccess, string message)> AddTendersJournal(long invId)
        {
            var terminalinfo = await GetTerminalInfo();
            if (terminalinfo == null)
                return (false, "Terminal information not found.");

            var invoice = await _dataContext.Invoice
                .Include(i => i.Cashier)
                .Include(i => i.EPayments)
                    .ThenInclude(ap => ap.SaleType)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == invId);

            if (invoice == null)
                return (false, "Invoice not found.");

            if (invoice.IsTrainMode)
                return (false, "Cannot add journal entries in training mode.");

            var journals = new List<Journal>();

            var status = invoice.Status == InvoiceStatusType.Paid ? "Posted" :
                invoice.Status == InvoiceStatusType.Returned ? "Returned" : "Unposted";

            if (invoice.CashTendered > 0)
            {

                journals.Add(new Journal
                {
                    Entry_No = invoice.InvoiceNumber.InvoiceFormat(),
                    Entry_Line_No = 0.ToString(),
                    Entry_Date = invoice.CreatedAt.DateFormat(),
                    CostCenter = terminalinfo.CostCenter,
                    ItemId = "",
                    Unit = "",
                    Qty = "0",
                    Cost = "",
                    Price = "",
                    TotalPrice = "",
                    Debit = invoice.Status == InvoiceStatusType.Returned ? "0" : invoice.CashTendered.StoreDecimalStringValueFormat(),
                    Credit = invoice.Status == InvoiceStatusType.Returned ? invoice.CashTendered.StoreDecimalStringValueFormat() : "0",
                    AccountBalance = "",
                    Prev_Reading = "",
                    Curr_Reading = "",
                    Memo = "Cash Tendered",
                    AccountName = "Cash",
                    Reference = invoice.InvoiceNumber.InvoiceFormat(),
                    Entry_Name = "1",
                    Cashier = invoice.Cashier.Email,
                    Count_Type = "",
                    Deposited = "0.00",
                    Deposit_Date = "",
                    Deposit_Reference = "",
                    Deposit_By = "",
                    Deposit_Time = "",
                    CustomerName = invoice.CustomerName,
                    SubTotal = "",
                    TotalTax = "",
                    GrossTotal = "",
                    Discount_Type = "",
                    Discount_Amount = "",
                    NetPayable = "",
                    Status = status,
                    User_Email = invoice.Cashier.Email,
                    QtyPerBaseUnit = "1",
                    QtyBalanceInBaseUnit = "0",
                    IsPushed = false
                });


            }

            if (invoice.EPayments != null)
            {
                foreach (var alternativePayment in invoice.EPayments)
                {
                    journals.Add(new Journal
                    {
                        Entry_No = invoice.InvoiceNumber.InvoiceFormat(),
                        Entry_Line_No = 0.ToString(),
                        Entry_Date = invoice.CreatedAt.DateFormat(),
                        CostCenter = terminalinfo.CostCenter,
                        ItemId = "",
                        Unit = "",
                        Qty = "0",
                        Cost = "",
                        Price = "",
                        TotalPrice = "",
                        Debit = invoice.Status == InvoiceStatusType.Returned ? "0" : alternativePayment.Amount.StoreDecimalStringValueFormat(),
                        Credit = invoice.Status == InvoiceStatusType.Returned ? alternativePayment.Amount.StoreDecimalStringValueFormat() : "0",
                        AccountBalance = "",
                        Prev_Reading = "",
                        Curr_Reading = "",
                        Memo = alternativePayment.SaleType.Name,
                        AccountName = alternativePayment.SaleType.Account,
                        Reference = invoice.InvoiceNumber.InvoiceFormat(),
                        Entry_Name = "1",
                        Cashier = invoice.Cashier.Email,
                        Count_Type = "",
                        Deposited = "0.00",
                        Deposit_Date = "",
                        Deposit_Reference = "",
                        Deposit_By = "",
                        Deposit_Time = "",
                        CustomerName = invoice.CustomerName,
                        SubTotal = "",
                        TotalTax = "",
                        GrossTotal = "",
                        Discount_Type = "",
                        Discount_Amount = "",
                        NetPayable = "",
                        Status = status,
                        User_Email = invoice.Cashier.Email,
                        QtyPerBaseUnit = "1",
                        QtyBalanceInBaseUnit = "0",
                        IsPushed = false
                    });

                }
            }

            _dataContext.AccountJournal.AddRange(journals);
            await _dataContext.SaveChangesAsync();

            return (true, "Payments journal entry successfully added.");
        }

        public async Task<(bool isSuccess, string message)> AddTotalsJournal(long invId)
        {
            var terminalinfo = await GetTerminalInfo();
            if (terminalinfo == null)
                return (false, "Terminal information not found.");

            var invoice = await _dataContext.Invoice
                .Include(i => i.Cashier)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == invId);

            if (invoice == null)
                return (false, "Invoice not found.");

            if (invoice.IsTrainMode)
                return (false, "Cannot add journal entries in training mode.");


            var status = invoice.Status == InvoiceStatusType.Paid ? "Posted" :
                invoice.Status == InvoiceStatusType.Returned ? "Returned" : "Unposted";

            _dataContext.AccountJournal.Add(new Journal
            {
                Entry_No = invoice.InvoiceNumber.InvoiceFormat(),
                Entry_Line_No = 10.ToString(),
                Entry_Date = invoice.CreatedAt.DateFormat(),
                CostCenter = terminalinfo.CostCenter,
                ItemId = "",
                Unit = "",
                Qty = "0",
                Cost = "0",
                Price = "0",
                TotalPrice = "",
                Debit = invoice.Status == InvoiceStatusType.Returned ? "0" : invoice.TotalAmount.StoreDecimalStringValueFormat(),
                Credit = invoice.Status == InvoiceStatusType.Returned ? invoice.TotalAmount.StoreDecimalStringValueFormat() : "0",
                AccountBalance = (invoice.Status == InvoiceStatusType.Returned ? -invoice.TotalAmount : invoice.TotalAmount).StoreDecimalStringValueFormat(),
                Prev_Reading = "",
                Curr_Reading = "",
                Memo = "Totals",
                AccountName = "Sales",
                Reference = invoice.InvoiceNumber.InvoiceFormat(),
                Entry_Name = "1",
                Cashier = invoice.Cashier.Email,
                Count_Type = "",
                Deposited = "0.00",
                Deposit_Date = "",
                Deposit_Reference = "",
                Deposit_By = "",
                Deposit_Time = "",
                CustomerName = invoice.CustomerName,
                SubTotal = invoice.SubTotal.StoreDecimalStringValueFormat(),
                TotalTax = invoice.VatAmount.StoreDecimalStringValueFormat(),
                GrossTotal = invoice.TotalAmount.StoreDecimalStringValueFormat(),
                Discount_Type = "",
                Discount_Amount = invoice.DiscountAmount.StoreDecimalStringValueFormat(),
                NetPayable = "",
                Status = status,
                User_Email = invoice.Cashier.Email,
                QtyPerBaseUnit = "1",
                QtyBalanceInBaseUnit = "0",
                IsPushed = false
            });

            await _dataContext.SaveChangesAsync();
            return (true, "Totals journal entry successfully added.");
        }

        public async Task<(bool isSuccess, string message)> PushJournals(DateTime selectedDate, IProgress<(int current, int total, string status)>? progress = null)
        {
            var dateString = selectedDate.ToString("yyyy-MM-dd");
            var journals = await _dataContext.AccountJournal
                .Where(d => d.Entry_Date == dateString && !d.IsPushed)
                .ToListAsync();

            int total = journals.Count, current = 0, errors = 0;
            var errorMessages = new List<string>();
            int batchSize = 100;
            int batchCounter = 0;

            progress?.Report((0, total, $"Found {total} entries to push for {dateString}."));

            foreach (var journal in journals)
            {
                try
                {
                    progress?.Report((current, total, $"Pushing {current + 1}/{total}"));
                    var url = $"asspos/mobilepostransactions.php?{ToQueryString(journal)}";
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    Debug.WriteLine(url);
                    var response = await _httpClient.GetAsync(url, cts.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        journal.IsPushed = true;
                        await Task.Delay(2000);
                    }
                    else
                    {
                        errors++;
                        errorMessages.Add($"Failed: {journal.UniqueId} ({response.StatusCode})");
                    }
                }
                catch (Exception ex)
                {
                    errors++;
                    errorMessages.Add($"Exception: {journal.UniqueId} ({ex.Message})");
                }
                current++;
                batchCounter++;
                progress?.Report((current, total, $"Pushing journal {current} of {total} to server..."));
                if (batchCounter % batchSize == 0)
                {
                    await _dataContext.SaveChangesAsync();
                    batchCounter = 0;
                }
                if (current < total) await Task.Delay(10);
            }

            if (journals.Any(j => j.IsPushed))
                await _dataContext.SaveChangesAsync();

            var isSuccess = errors == 0;
            var message = isSuccess
                ? $"All {total} journals pushed successfully."
                : $"{total - errors} journals pushed, {errors} errors. {string.Join("; ", errorMessages)}";
            return (isSuccess, message);

            static string ToQueryString(Journal journal)
            {
                return string.Join("&", typeof(Journal).GetProperties()
                    .Where(p => p.Name != nameof(Journal.UniqueId) && p.Name != nameof(Journal.IsPushed))
                    .Select(p => $"{p.Name.ToLowerInvariant()}={Uri.EscapeDataString(p.GetValue(journal)?.ToString() ?? "")}"));
            }
        }
    }
}
