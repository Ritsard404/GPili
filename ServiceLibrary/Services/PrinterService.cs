using ServiceLibrary.Data;
using ServiceLibrary.Services.DTO.Report;
using ServiceLibrary.Services.Interfaces;
using ServiceLibrary.Utils;
using System.Diagnostics;
using System.Text;

namespace ServiceLibrary.Services
{
    public interface IPrinterService
    {
        Task PrintXReading();
        Task PrintZReading();
        Task CashTrack();
        Task PrintInvoice(InvoiceDTO invoiceInfo);
        Task ReprintInvoice(InvoiceDTO invoiceInfo);
    }

    public class PrinterService(DataContext _dataContext, IGPiliTerminalMachine _terminalMachine) : IPrinterService
    {
        private const int ReceiptWidth = 32;
        private const int QtyWidth = 3;
        private const int DescWidth = 20;
        private const int AmountWidth = 9;

        private string CenterText(string text) =>
            text.PadLeft((ReceiptWidth + text.Length) / 2).PadRight(ReceiptWidth);
        private string AlignText(string left, string right) =>
            left.PadRight(ReceiptWidth - (right ?? "0").Length) + (right ?? "0");

        private string FormatItemLine(string qty, string desc, string amount) =>
             $"{qty.PadRight(QtyWidth)}{desc.PadRight(DescWidth)}{amount.PadLeft(AmountWidth)}";

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        private async void PrintToPrinter(StringBuilder content)
        {
            var info = await _terminalMachine.GetTerminalInfo();
            var printerName = info?.PrinterName
                ?? throw new InvalidOperationException("PrinterName not set on terminal.");

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

        public Task CashTrack()
        {
            throw new NotImplementedException();
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
            content.AppendLine(CenterText($"{"Total:",-15}{invoiceInfo.TotalAmount,17}"))
                // #TODO To Add Discount
                .AppendLine(CenterText($"{"Due Amount:",-15}{invoiceInfo.DueAmount,17}"))
                .AppendLine(CenterText($"{"Due Amount:",-15}{invoiceInfo.DueAmount,17}"));

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
            if (!string.IsNullOrEmpty(invoiceInfo.ElligiblePersonDiscount)|| invoiceInfo.OtherPayments.Count > 0)
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
        }

        public Task PrintXReading()
        {
            throw new NotImplementedException();
        }

        public Task PrintZReading()
        {
            throw new NotImplementedException();
        }

        public Task ReprintInvoice(InvoiceDTO invoiceInfo)
        {
            throw new NotImplementedException();
        }
    }
}
