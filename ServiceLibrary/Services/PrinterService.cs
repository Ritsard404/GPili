using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServiceLibrary.Data;
using ServiceLibrary.Models;
using ServiceLibrary.Services.DTO.Report;
using ServiceLibrary.Services.Interfaces;
using ServiceLibrary.Utils;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;
using static ServiceLibrary.Utils.FolderPath;

namespace ServiceLibrary.Services
{
    public interface IPrinterService
    {
        Task PrintXReading();
        Task PrintZReading();
        Task<(bool isSuccess, string message)> ReprintPrintXReading(long id);
        Task<(bool isSuccess, string message)> ReprintPrintZReading(long id);
        void PrintCashTrack(string CashInDrawer, string CurrentCashDrawer, string cashierName);
        Task PrintInvoice(InvoiceDTO invoiceInfo);
        Task<(bool isSuccess, string message)> ReprintInvoice(long id);
    }

    public class PrinterService(DataContext _dataContext,
        IGPiliTerminalMachine _terminalMachine,
        IReport _report) : IPrinterService
    {
        private const int ReceiptWidth = 32;
        private const int QtyWidth = 5;
        private const int DescWidth = 18;
        private const int AmountWidth = 9;

        private string CenterText(string text) =>
            text.PadLeft((ReceiptWidth + text.Length) / 2).PadRight(ReceiptWidth);
        private string AlignText(string left, string right) =>
            left.PadRight(ReceiptWidth - (right ?? "0").Length) + (right ?? "0");
        private string AlignLabelAmount(string label, string amount, int width)
        {
            // If label + amount fits, print on one line
            if (label.Length + amount.Length + 1 <= width)
            {
                return label.PadRight(width - amount.Length) + amount;
            }
            else
            {
                // Print label on one line, amount right-aligned on next
                return label + Environment.NewLine + amount.PadLeft(width);
            }
        }
        private string FormatItemLine(string qty, string desc, string amount) =>
             $"{qty.PadRight(QtyWidth)}{desc.PadRight(DescWidth)}{amount.PadLeft(AmountWidth)}";

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        private string GetDefaultPrinterName()
        {
#if WINDOWS
    using var settings = new PrinterSettings();
    return settings.PrinterName;
#else
            throw new PlatformNotSupportedException(
                "Default printer detection is only supported on Windows.");
#endif
        }

        private async void PrintToPrinter(StringBuilder content)
        {
            // fetch terminal preference
            string printerName = null;
            try
            {
                var info = await _terminalMachine.GetTerminalInfo();
                printerName = info?.PrinterName;
            }
            catch
            {
                // ignore and fall back
            }

            if (string.IsNullOrWhiteSpace(printerName))
            {
                // reliable fallback
                printerName = GetDefaultPrinterName();
            }

            // Add line feeds for paper cutting
            content.AppendLine("\n\n\n");
            RawPrinterHelper.PrintText(printerName, content.ToString());
        }

        private async Task<bool> isAcknowledgementInvoice()
        {
            var info = await _terminalMachine.GetTerminalInfo();
            if (info == null)
                return true;

            return info.Vat <= 0;
        }

        public async Task PrintInvoice(InvoiceDTO invoiceInfo)
        {
            var folderPath = FolderPath.SalesReport.Invoices;
            var filePath = Path.Combine(folderPath, $"{invoiceInfo.InvoiceNum} {invoiceInfo.InvoiceDate}.txt");
            var isTrainMode = invoiceInfo.BusinesDetails.IsTrainMode;

            EnsureDirectoryExists(folderPath);

            var content = new StringBuilder();

            if (isTrainMode)
            {
                content.AppendLine(CenterText("TRAIN MODE"))
                    .AppendLine();
            }

            if (await isAcknowledgementInvoice())
            {
                content.AppendLine(new string('=', ReceiptWidth))
                    .AppendLine(CenterText("Acknowledgment Receipt"))
                    .AppendLine(new string('=', ReceiptWidth));
            }
            else
            {
                content.AppendLine(new string('=', ReceiptWidth))
                    .AppendLine(CenterText("INVOICE"))
                    .AppendLine(new string('=', ReceiptWidth))
                    .AppendLine(CenterText(invoiceInfo.BusinesDetails.RegisteredName))
                    .AppendLine(CenterText(invoiceInfo.BusinesDetails.Address))
                    .AppendLine(CenterText($"TIN: {invoiceInfo.BusinesDetails.VatTinNumber}"))
                    .AppendLine(CenterText($"MIN: {invoiceInfo.BusinesDetails.MinNumber}"))
                    .AppendLine(new string('-', ReceiptWidth));
            }


            // Invoice details
            content.AppendLine()
                .AppendLine($"INV: {invoiceInfo.InvoiceNum}".PadRight(ReceiptWidth))
                .AppendLine()
                .AppendLine($"Date: {invoiceInfo.InvoiceDate:d}".PadRight(ReceiptWidth))
                .AppendLine($"Cashier: {invoiceInfo.CashierName}".PadRight(ReceiptWidth))
                .AppendLine(new string('-', ReceiptWidth))

            // Items header
                .AppendLine(FormatItemLine("Qty", "Description", "Amount"))
                .AppendLine(new string('-', ReceiptWidth))
                .AppendLine();

            // Invoice Items
            foreach (var item in invoiceInfo.Items)
            {
                content.AppendLine(FormatItemLine(
                    item.Qty,
                    item.Description.Length > DescWidth
                        ? item.Description.Substring(0, DescWidth)
                        : item.Description,
                    item.Amount));
            }
            content.AppendLine(new string('-', ReceiptWidth));

            // Totals
            content.AppendLine(CenterText($"{"Total:",-15}{invoiceInfo.TotalAmount,17}"));
            // #TODO To Add Discount
            //.AppendLine(CenterText($"{"Sub Total:",-15}{invoiceInfo.SubTotal,17}"))
            if (!string.IsNullOrEmpty(invoiceInfo.ElligiblePersonDiscount) || invoiceInfo.OtherPayments.Count > 0)
                content.AppendLine(AlignLabelAmount($"Discount({invoiceInfo.DiscountType}):", invoiceInfo.DiscountAmount, ReceiptWidth));
            content.AppendLine(CenterText($"{"Due Amount:",-15}{invoiceInfo.DueAmount,17}"));

            // Other Payments
            if (invoiceInfo.OtherPayments.Count > 0)
            {
                foreach (var payment in invoiceInfo.OtherPayments)
                {
                    content.AppendLine(CenterText($"{$"{payment.SaleTypeName}:",-15}{payment.Amount,17}"));
                }
            }

            content.AppendLine(CenterText($"{"Cash:",-15}{invoiceInfo.CashTenderAmount,17}"))
                .AppendLine(CenterText($"{"Total Tender:",-15}{invoiceInfo.TotalTenderAmount,17}"))
                .AppendLine(CenterText($"{"Change:",-15}{invoiceInfo.ChangeAmount,17}"))
                .AppendLine()
                .AppendLine(CenterText($"{"Vat Zero:",-15}{invoiceInfo.VatZero,17}"))
                .AppendLine(CenterText($"{"Vat Exempt:",-15}{invoiceInfo.VatExemptSales,17}"))
                .AppendLine(CenterText($"{"Vat Sales:",-15}{invoiceInfo.VatSales,17}"))
                .AppendLine(CenterText($"{"Vat Amount:",-15}{invoiceInfo.VatAmount,17}"))
                .AppendLine();

            if (string.IsNullOrEmpty(invoiceInfo.ElligiblePersonDiscount))
            {
                content.AppendLine("Name:_________________")
                    .AppendLine("Address:______________")
                    .AppendLine("TIN: _________________")
                    .AppendLine("Signature: ___________")
                    .AppendLine();
            }
            else
            {
                content.AppendLine($"Name: {invoiceInfo.ElligiblePersonDiscount}")
                    .AppendLine("Address:______________")
                    .AppendLine("TIN: _________________")
                    .AppendLine("Signature: ___________")
                    .AppendLine();
            }

            // Print Copies
            if (!string.IsNullOrEmpty(invoiceInfo.ElligiblePersonDiscount) || invoiceInfo.OtherPayments.Count > 0)
            {
                // Store original content once
                string baseContent = content.ToString();

                foreach (var label in new[] { "", "COPY" })
                {
                    // Create a fresh builder for each output
                    var contentWithLabel = new StringBuilder();

                    // Add label if not empty
                    if (!string.IsNullOrWhiteSpace(label))
                    {
                        contentWithLabel.AppendLine(CenterText($"*** {label} ***"));
                    }

                    contentWithLabel.Append(baseContent);

                    var baseName = Path.GetFileNameWithoutExtension(filePath);
                    var ext = Path.GetExtension(filePath);

                    var outName = string.IsNullOrWhiteSpace(label)
                        ? $"{baseName}{ext}"
                        : $"{baseName}_{label}{ext}";

                    var outPath = Path.Combine(folderPath, outName);

                    File.WriteAllText(outPath, contentWithLabel.ToString());
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });

                    // Print to thermal printer
                    //PrintToPrinter(contentWithLabel);
                }
            }
            else
            {
                File.WriteAllText(filePath, content.ToString());
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });

                // Print to thermal printer
                //PrintToPrinter(content);
            }


            var receiptBytes = Encoding.UTF8.GetBytes(content.ToString());
            var invoiceNumber = long.Parse(invoiceInfo.InvoiceNum);
            var invoice = await _dataContext.Invoice.FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber);

            var invoiceDocument = new InvoiceDocument
            {
                InvoiceBlob = receiptBytes,
                Type = InvoiceDocumentType.Invoice, // or "X", "Y", etc. as needed
                Invoice = invoice,
            };

            _dataContext.InvoiceDocument.Add(invoiceDocument);
            await _dataContext.SaveChangesAsync();
        }

        public async Task PrintXReading()
        {
            var xInvoice = await _report.GetXInvoice();

            var reportPath = SalesReport.XInvoiceReports;

            EnsureDirectoryExists(reportPath);

            string fileName = $"XInvoice-{DateTime.UtcNow.ToString("MMMM-dd-yyyy-HH-mm-ss")}.txt";
            var filePath = Path.Combine(reportPath, fileName);

            var isTrainMode = xInvoice.IsTrainMode;

            var content = new StringBuilder();

            if (isTrainMode)
            {
                content.AppendLine(CenterText("TRAIN MODE"))
                    .AppendLine();
            }

            if (!await isAcknowledgementInvoice())
            {
                // Header
                content.AppendLine(CenterText(xInvoice.BusinessName))
                    .AppendLine(CenterText($"Operated by: {xInvoice.OperatorName}"))
                    .AppendLine()
                    .AppendLine(CenterText(xInvoice.AddressLine))
                    .AppendLine()
                    .AppendLine(CenterText($"VAT REG TIN: {xInvoice.VatRegTin}"))
                    .AppendLine(CenterText($"MIN: {xInvoice.Min}"))
                    .AppendLine(CenterText($"S/N: {xInvoice.SerialNumber}"))
                    .AppendLine();
            }


            // Title
            content.AppendLine(CenterText("X-READING REPORT"))
                .AppendLine();

            // Report date/time
            content.AppendLine(AlignText("Report Date:", xInvoice.ReportDate))
                .AppendLine(AlignText("Report Time:", xInvoice.ReportTime))
                .AppendLine();

            // Period
            content.AppendLine(AlignText("Start Date/Time:", xInvoice.StartDateTime))
                .AppendLine(AlignText("End Date/Time:", xInvoice.EndDateTime))
                .AppendLine();

            // Cashier & OR
            content.AppendLine(AlignText($"Cashier: {xInvoice.Cashier}", ""))
                .AppendLine()
                .AppendLine(AlignText("Beg. OR #:", xInvoice.BeginningOrNumber))
                .AppendLine(AlignText("End. OR #:", xInvoice.EndingOrNumber))
                .AppendLine(AlignText("Txn Count #:", xInvoice.TransactCount))
                .AppendLine();

            // Opening fund
            content.AppendLine(AlignText("Opening Fund:", xInvoice.OpeningFund))
                .AppendLine(new string('=', ReceiptWidth));

            // Payments section
            content.AppendLine(CenterText("PAYMENTS RECEIVED"))
                .AppendLine()
                .AppendLine(AlignText("CASH", xInvoice.Payments.CashString));
            if (xInvoice.Payments.OtherPayments != null)
            {
                foreach (var p in xInvoice.Payments.OtherPayments)
                {
                    content.AppendLine(AlignText(p.Name.ToUpper(), p.AmountString));
                }
            }
            content.AppendLine(AlignText("Total Payments:", xInvoice.Payments.Total))
                .AppendLine(new string('=', ReceiptWidth));

            // Void / Refund / Withdrawal
            content.AppendLine(AlignText($"VOID ({xInvoice.VoidCount})", xInvoice.VoidAmount))
                .AppendLine(new string('=', ReceiptWidth))
                .AppendLine(AlignText($"REFUND ({xInvoice.RefundCount})", xInvoice.Refund))
                .AppendLine(new string('=', ReceiptWidth))
                .AppendLine(AlignText("WITHDRAWAL", xInvoice.Withdrawal))
                .AppendLine(new string('=', ReceiptWidth));

            // Transaction summary
            content.AppendLine(CenterText("TRANSACTION SUMMARY"))
                .AppendLine()
                .AppendLine(AlignText("Cash In Drawer:", xInvoice.TransactionSummary.CashInDrawer));
            foreach (var p in xInvoice.TransactionSummary.OtherPayments)
            {
                content.AppendLine(AlignText(p.Name.ToUpper(), p.AmountString));
            }
            content.AppendLine(new string('=', ReceiptWidth));

            // Short/Over
            content.AppendLine(AlignText("SHORT/OVER:", xInvoice.ShortOver))
                .AppendLine();

            var receiptBytes = Encoding.UTF8.GetBytes(content.ToString());

            var invoiceDocument = new InvoiceDocument
            {
                InvoiceBlob = receiptBytes,
                Type = InvoiceDocumentType.XReport, // or "X", "Y", etc. as needed
            };

            _dataContext.InvoiceDocument.Add(invoiceDocument);
            await _dataContext.SaveChangesAsync();

            // Save to file
            File.WriteAllText(filePath, content.ToString());

            // Print to thermal printer
            //PrintToPrinter(content);

            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }

        public async Task PrintZReading()
        {
            var zInvoice = await _report.GetZInvoice();

            var reportPath = SalesReport.ZInvoiceReports;

            EnsureDirectoryExists(reportPath);

            string fileName = $"ZInvoice-{DateTime.Now:MMMM-dd-yyyy-HH-mm-ss}.txt";
            var filePath = Path.Combine(reportPath, fileName);

            var content = new StringBuilder();

            if (!await isAcknowledgementInvoice())
            {

                // Header
                content.AppendLine(CenterText(zInvoice.BusinessName))
                    .AppendLine(CenterText($"Operated by: {zInvoice.OperatorName}"))
                    .AppendLine(CenterText(zInvoice.AddressLine))
                    .AppendLine(CenterText($"VAT REG TIN: {zInvoice.VatRegTin}"))
                    .AppendLine(CenterText($"MIN: {zInvoice.Min}"))
                    .AppendLine(CenterText($"S/N: {zInvoice.SerialNumber}"))
                    .AppendLine();
            }

            // Title
            content.AppendLine(CenterText("Z-READING REPORT"))
                .AppendLine();

            // Report date/time
            content.AppendLine(AlignText("Report Date:", zInvoice.ReportDate))
                .AppendLine(AlignText("Report Time:", zInvoice.ReportTime))
                .AppendLine();

            // Period
            content.AppendLine(AlignText("Start Date/Time:", zInvoice.StartDateTime))
                .AppendLine(AlignText("End Date/Time:", zInvoice.EndDateTime))
                .AppendLine();

            // SI/VOID/RETURN numbers
            content.AppendLine(AlignText("Beg. SI #:", zInvoice.BeginningSI))
                .AppendLine(AlignText("End. SI #:", zInvoice.EndingSI))
                .AppendLine(AlignText("Beg. VOID #:", zInvoice.BeginningVoid))
                .AppendLine(AlignText("End. VOID #:", zInvoice.EndingVoid))
                .AppendLine(AlignText("Beg. RETURN #:", zInvoice.BeginningReturn))
                .AppendLine(AlignText("End. RETURN #:", zInvoice.EndingReturn))
                .AppendLine();

            content.AppendLine(AlignText("Txn Count #:", zInvoice.TransactCount))
                .AppendLine(AlignText("Reset Counter No.:", zInvoice.ResetCounter))
                .AppendLine(AlignText("Z Counter No.:", zInvoice.ZCounter))
                .AppendLine(new string('-', ReceiptWidth));

            // Sales section
            content.AppendLine(AlignText("Accum. Sales:", zInvoice.PresentAccumulatedSales))
                .AppendLine(AlignText("Prev. Accum. Sales:", zInvoice.PreviousAccumulatedSales))
                .AppendLine(AlignText("Sales for the Day:", zInvoice.SalesForTheDay))
                .AppendLine(new string('-', ReceiptWidth));

            // Breakdown of sales
            content.AppendLine(CenterText("BREAKDOWN OF SALES"))
                .AppendLine()
                .AppendLine(AlignText("VATABLE SALES:", zInvoice.SalesBreakdown.VatableSales))
                .AppendLine(AlignText("VAT AMOUNT:", zInvoice.SalesBreakdown.VatAmount))
                .AppendLine(AlignText("VAT EXEMPT SALES:", zInvoice.SalesBreakdown.VatExemptSales))
                .AppendLine(AlignText("ZERO RATED SALES:", zInvoice.SalesBreakdown.ZeroRatedSales))
                .AppendLine(new string('-', ReceiptWidth))
                .AppendLine(AlignText("Gross Amount:", zInvoice.SalesBreakdown.GrossAmount))
                .AppendLine(AlignText("Less Discount:", zInvoice.SalesBreakdown.LessDiscount))
                .AppendLine(AlignText("Less Return:", zInvoice.SalesBreakdown.LessReturn))
                .AppendLine(AlignText("Less Void:", zInvoice.SalesBreakdown.LessVoid))
                .AppendLine(AlignText("Less VAT Adjustment:", zInvoice.SalesBreakdown.LessVatAdjustment))
                .AppendLine(AlignText("Net Amount:", zInvoice.SalesBreakdown.NetAmount))
                .AppendLine(new string('-', ReceiptWidth));

            // Discounts
            content.AppendLine(CenterText("DISCOUNT SUMMARY"))
                .AppendLine(AlignText($"SC Disc. ({zInvoice.DiscountSummary.SeniorCitizenCount}):", zInvoice.DiscountSummary.SeniorCitizen))
                .AppendLine(AlignText($"PWD Disc. ({zInvoice.DiscountSummary.PWDCount}):", zInvoice.DiscountSummary.PWDCount))
                .AppendLine(AlignText($"Other Disc. ({zInvoice.DiscountSummary.OtherCount}):", zInvoice.DiscountSummary.Other))
                .AppendLine(new string('-', ReceiptWidth));

            // Adjustments
            content.AppendLine(CenterText("SALES ADJUSTMENT"))
                .AppendLine(AlignText($"VOID ({zInvoice.SalesAdjustment.VoidCount}):", zInvoice.SalesAdjustment.Void))
                .AppendLine(AlignText($"RETURN ({zInvoice.SalesAdjustment.ReturnCount}):", zInvoice.SalesAdjustment.Return))
                .AppendLine(new string('-', ReceiptWidth));

            content.AppendLine(CenterText("VAT ADJUSTMENT"))
                .AppendLine(AlignText("SC TRANS. :", zInvoice.VatAdjustment.SCTrans))
                .AppendLine(AlignText("PWD TRANS :", zInvoice.VatAdjustment.PWDTrans))
                .AppendLine(AlignText("REG.Disc. TRANS :", zInvoice.VatAdjustment.RegDiscTrans))
                .AppendLine(AlignText("ZERO-RATED TRANS.:", zInvoice.VatAdjustment.ZeroRatedTrans))
                .AppendLine(AlignText("VAT on Return:", zInvoice.VatAdjustment.VatOnReturn))
                .AppendLine(AlignText("Other VAT Adjustments:", zInvoice.VatAdjustment.OtherAdjustments))
                .AppendLine(new string('-', ReceiptWidth));

            // Transaction Summary
            content.AppendLine(CenterText("TRANSACTION SUMMARY"))
                .AppendLine()
                .AppendLine(AlignText("Cash In Drawer:", zInvoice.TransactionSummary.CashInDrawer));
            foreach (var p in zInvoice.TransactionSummary.OtherPayments)
            {
                content.AppendLine(AlignText(p.Name.ToUpper(), p.AmountString));
            }
            content.AppendLine(AlignText("Opening Fund:", zInvoice.OpeningFund))
                .AppendLine(AlignText("Less Withdrawal:", zInvoice.Withdrawal))
                .AppendLine(AlignText("Payments Received:", zInvoice.PaymentsReceived))
                .AppendLine(new string('-', ReceiptWidth));

            // Short/Over
            content.AppendLine(AlignText("SHORT/OVER:", zInvoice.ShortOver))
                .AppendLine();

            var receiptBytes = Encoding.UTF8.GetBytes(content.ToString());

            var invoiceDocument = new InvoiceDocument
            {
                InvoiceBlob = receiptBytes,
                Type = InvoiceDocumentType.ZReport, // or "X", "Y", etc. as needed
            };

            _dataContext.InvoiceDocument.Add(invoiceDocument);
            await _dataContext.SaveChangesAsync();

            // Save to file
            File.WriteAllText(filePath, content.ToString());

            // Print to thermal printer
            //PrintToPrinter(content);

            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }

        public async Task<(bool isSuccess, string message)> ReprintInvoice(long id)
        {
            // Find the latest InvoiceDocument for this invoice (type "Invoice")
            var invoiceDocument = await _dataContext.InvoiceDocument
                .Where(d => d.Invoice != null &&
                        d.Id == d.Id &&
                        d.Type == InvoiceDocumentType.Invoice)
                .OrderByDescending(d => d.Id)
                .FirstOrDefaultAsync();

            if (invoiceDocument?.InvoiceBlob == null)
                return (false, "No stored invoice found for this ID.");

            // Convert blob to string
            var content = Encoding.UTF8.GetString(invoiceDocument.InvoiceBlob);

            var sb = new StringBuilder(content);
            sb.AppendLine()
              .AppendLine(new string('=', ReceiptWidth))
              .AppendLine($"Reprint #{invoiceDocument.ReprintCount}")
              .AppendLine($"Date {DateTime.Now:yyyy/MM/dd HH:mm:ss}")
              .AppendLine(new string('=', ReceiptWidth));

            // When you need the final string:
            var finalContent = sb.ToString();

            // Create a temporary file path
            var tempPath = Path.Combine(Path.GetTempPath(), $"INV{id.InvoiceFormat()}_{DateTime.Now:yyyyMMddHHmmss}.txt");

            invoiceDocument.ReprintCount++;
            await _dataContext.SaveChangesAsync();

            // Print to thermal printer
            //PrintToPrinter(content);

            File.WriteAllText(tempPath, content);
            Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });

            return (true, "Invoice reprinted successfully.");
        }

        public async Task<(bool isSuccess, string message)> ReprintPrintXReading(long id)
        {
            var invoiceDocument = await _dataContext.InvoiceDocument
                .Where(d => d.Id == id &&
                        d.Type == InvoiceDocumentType.XReport)
                .OrderByDescending(d => d.Id)
                .FirstOrDefaultAsync();

            if (invoiceDocument?.InvoiceBlob == null)
                return (false, "No stored X-Reading report found for this ID.");

            // Convert blob to string
            var content = Encoding.UTF8.GetString(invoiceDocument.InvoiceBlob);

            var sb = new StringBuilder(content);
            sb.AppendLine()
              .AppendLine(new string('=', ReceiptWidth))
              .AppendLine($"Reprint #{invoiceDocument.ReprintCount}")
              .AppendLine($"Date {DateTime.Now:yyyy/MM/dd HH:mm:ss}")
              .AppendLine(new string('=', ReceiptWidth));

            // When you need the final string:
            var finalContent = sb.ToString();

            // Create a temporary file path
            var tempPath = Path.Combine(Path.GetTempPath(), $"XReport_{invoiceDocument.CreatedAt:yyyyMMddHHmmss}.txt");

            invoiceDocument.ReprintCount++;
            await _dataContext.SaveChangesAsync();

            // Print to thermal printer
            //PrintToPrinter(content);

            File.WriteAllText(tempPath, content);
            Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });

            return (true, "X-Reading report reprinted successfully.");
        }

        public async Task<(bool isSuccess, string message)> ReprintPrintZReading(long id)
        {
            var invoiceDocument = await _dataContext.InvoiceDocument
                .Where(d => d.Id == id &&
                        d.Type == InvoiceDocumentType.ZReport)
                .OrderByDescending(d => d.Id)
                .FirstOrDefaultAsync();

            if (invoiceDocument?.InvoiceBlob == null)
                return (false, "No stored Z-Reading report found for this ID.");

            // Convert blob to string
            var content = Encoding.UTF8.GetString(invoiceDocument.InvoiceBlob);

            var sb = new StringBuilder(content);
            sb.AppendLine()
              .AppendLine(new string('=', ReceiptWidth))
              .AppendLine($"Reprint #{invoiceDocument.ReprintCount}")
              .AppendLine($"Date {DateTime.Now:yyyy/MM/dd HH:mm:ss}")
              .AppendLine(new string('=', ReceiptWidth));

            // When you need the final string:
            var finalContent = sb.ToString();

            // Create a temporary file path
            var tempPath = Path.Combine(Path.GetTempPath(), $"ZReport_{invoiceDocument.CreatedAt:yyyyMMddHHmmss}.txt");

            invoiceDocument.ReprintCount++;
            await _dataContext.SaveChangesAsync();

            // Print to thermal printer
            //PrintToPrinter(content);

            File.WriteAllText(tempPath, content);
            Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });

            return (true, "Z-Reading report reprinted successfully.");
        }

        public void PrintCashTrack(string cashInDrawer, string currentCashDrawer, string cashierName)
        {


            // Build the report content
            var sb = new StringBuilder();
            sb.AppendLine(new string('=', ReceiptWidth));
            sb.AppendLine(CenterText("Cash Track Report"));
            sb.AppendLine(new string('=', ReceiptWidth));
            sb.AppendLine($"Cash In Drawer: {cashInDrawer}");
            sb.AppendLine($"Total Cash Drawer: {currentCashDrawer}");

            string reportContent = sb.ToString();

            // Archive as a .txt file
            var cashTracksPath = SalesReport.CashTracks;
            if (!string.IsNullOrWhiteSpace(cashTracksPath))
            {
                EnsureDirectoryExists(cashTracksPath);

                string fileName = $"Cash-Track-{cashierName}-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.txt";
                string filePath = Path.Combine(cashTracksPath, fileName);
                File.WriteAllText(filePath, reportContent);
            }

            // Prepare print content (add line feeds for paper cutting)
            string printContent = reportContent + "\n\n\n";

            // Print to default text viewer (or send to printer)
            var tempPrintPath = Path.Combine(Path.GetTempPath(), $"CashTrack_{cashierName}_{DateTime.Now:yyyyMMddHHmmss}.txt");
            File.WriteAllText(tempPrintPath, printContent);
            Process.Start(new ProcessStartInfo(tempPrintPath) { UseShellExecute = true });

            // Optionally, print to thermal printer
            PrintToPrinter(new StringBuilder(printContent));
        }
    }
}
