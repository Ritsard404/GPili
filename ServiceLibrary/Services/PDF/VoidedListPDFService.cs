using PdfSharp.Drawing;
using PdfSharp.Pdf;
using ServiceLibrary.Services.DTO.Report;
using ServiceLibrary.Services.Interfaces;
using System.Net;
using System.Text;

namespace ServiceLibrary.Services.PDF
{
    public class VoidedListPDFService(IGPiliTerminalMachine _terminalMachine)
    {
        static VoidedListPDFService()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
        public async Task<byte[]> GenerateVoidedListPDF(List<VoidedListDTO> voidedLists, TotalVoidedListDTO totalVoided, DateTime fromDate, DateTime toDate)
        {
            var posInfo = await _terminalMachine.GetTerminalInfo();

            using var document = new PdfDocument();
            var page = document.AddPage();
            page.Orientation = PdfSharp.PageOrientation.Landscape;
            page.Width = XUnit.FromInch(13.0);
            page.Height = XUnit.FromInch(8.5);
            var gfx = XGraphics.FromPdfPage(page);
            // Fonts
            var titleFont = new XFont("Arial", 16, XFontStyle.Bold);
            var headerFont = new XFont("Arial", 10, XFontStyle.Bold);
            var normalFont = new XFont("Arial", 9, XFontStyle.Regular);
            var smallFont = new XFont("Arial", 8, XFontStyle.Regular);

            double y = 40;
            double margin = 30;
            double tableTop = 0;
            double pageWidth = page.Width - margin * 2;

            // Header
            gfx.DrawString(posInfo.RegisteredName, titleFont, XBrushes.DarkBlue, new XPoint(margin, y));
            y += 18;
            gfx.DrawString(posInfo.Address, normalFont, XBrushes.Black, new XPoint(margin, y));
            y += 12;
            gfx.DrawString($"TIN {posInfo.VatTinNumber}", normalFont, XBrushes.Black, new XPoint(margin, y));
            y += 18;
            gfx.DrawString("VOIDED LIST REPORT", headerFont, XBrushes.DarkBlue, new XPoint(margin, y));
            y += 14;
            gfx.DrawString($"From {fromDate:MM-dd-yyyy} To {toDate:MM-dd-yyyy}", normalFont, XBrushes.Black, new XPoint(margin, y));
            y += 18;
            tableTop = y;

            // Table columns (sum of fractions = 1.0)
            var columns = new[]
                {
                    ("DATE", 0.06),
                    ("OR NO", 0.09),
                    ("DISC\nTYPE", 0.06),
                    ("%", 0.03),
                    ("GROSS\nSALES", 0.06),
                    ("DISCOUNT", 0.06),
                    ("AMOUNT\nDUE", 0.06),
                    ("NET OF\nSALES", 0.06),
                    ("VATABLE", 0.05),
                    ("ZERO\nRATED", 0.04),
                    ("EXEMPT", 0.04),
                    ("REASON", 0.12),         // Increased
                    ("USER", 0.09),           // Increased
                    ("CANCELLED\nBY", 0.09),  // Increased
                    ("CANCELLED\nDATE", 0.06),
                    ("CANCELLED\nTIME", 0.06)
                };
            double[] colWidths = columns.Select(c => c.Item2 * pageWidth).ToArray();
            double headerRowHeight = 30;
            double rowHeight = 18;
            var formats = new[]
            {
                XStringFormats.Center,
                XStringFormats.CenterLeft,
                XStringFormats.Center,
                XStringFormats.CenterLeft,
                XStringFormats.Center,
                XStringFormats.CenterRight,
                XStringFormats.CenterRight,
                XStringFormats.CenterRight,
                XStringFormats.CenterRight,
                XStringFormats.CenterRight,
                XStringFormats.CenterRight,
                XStringFormats.CenterRight,
                XStringFormats.CenterLeft,
                XStringFormats.Center,
                XStringFormats.Center,
                XStringFormats.Center,
                XStringFormats.Center
            };

            // Draw table headers
            double x = margin;
            for (int i = 0; i < columns.Length; i++)
            {
                var rect = new XRect(x, y, colWidths[i], headerRowHeight);
                gfx.DrawRectangle(XBrushes.LightGray, rect);
                var headerLines = columns[i].Item1.Split('\n');
                double lineHeight = headerRowHeight / headerLines.Length;
                for (int j = 0; j < headerLines.Length; j++)
                {
                    var lineRect = new XRect(x, y + j * lineHeight, colWidths[i], lineHeight);
                    gfx.DrawString(headerLines[j], smallFont, XBrushes.Black, lineRect, XStringFormats.Center);
                }
                x += colWidths[i];
            }
            y += headerRowHeight;

            // Draw table rows (parent and child)
            foreach (var item in voidedLists)
            {
                x = margin;
                string[] row = new string[]
                {
                    item.Date,
                    item.InvoiceNum,
                    item.DiscType,
                    item.Percent,
                    item.GrossSales.ToString("N2"),
                    item.Discount.ToString("N2"),
                    item.AmountDue.ToString("N2"),
                    (item.AmountDue - item.Discount).ToString("N2"),
                    item.Vatable.ToString("N2"),
                    item.ZeroRated.ToString("N2"),
                    item.Exempt.ToString("N2"),
                    item.Reason ?? string.Empty,
                    item.User,
                    item.CancelledBy,
                    item.CancelledDate,
                    item.CancelledTime
                };

                for (int i = 0; i < row.Length; i++)
                {
                    var rect = new XRect(x, y, colWidths[i], rowHeight);
                    gfx.DrawString(row[i], smallFont, XBrushes.Black, rect, formats[i]);
                    x += colWidths[i];
                }
                y += rowHeight;
                // Draw row line
                gfx.DrawLine(XPens.Gray, margin, y, margin + pageWidth, y);

                // Add vertical space before child rows
                y += 6;

                // Draw child rows (VoidedItemList)
                if (item.VoidedItemList != null && item.VoidedItemList.Count > 0)
                {
                    // Draw sub-header for items
                    double childIndent = 40;
                    double childX = margin + childIndent; // Indent
                    double[] childColWidths = { 40, 80, 200, 60, 60, 70, 70, 120 };
                    string[] childHeaders = { "No", "Barcode", "Item Name", "Qty", "Price", "Amount", "Return", "Reason" };
                    for (int i = 0; i < childHeaders.Length; i++)
                    {
                        var rect = new XRect(childX, y, childColWidths[i], rowHeight - 4);
                        gfx.DrawRectangle(XBrushes.LightGray, rect);
                        gfx.DrawString(childHeaders[i], smallFont, XBrushes.Black, rect, XStringFormats.Center);
                        childX += childColWidths[i];
                    }
                    y += rowHeight - 4;

                    // Draw each voided item
                    foreach (var child in item.VoidedItemList)
                    {
                        childX = margin + childIndent;
                        string[] childRow =
                        {
                            child.No.ToString(),
                            child.Barcode,
                            child.ItemName,
                            child.Quantity.ToString("N2"),
                            child.Price.ToString("N2"),
                            child.Amount.ToString("N2"),
                            child.Return.ToString("N2"),
                            child.Reason ?? string.Empty
                        };
                        for (int i = 0; i < childRow.Length; i++)
                        {
                            var rect = new XRect(childX, y, childColWidths[i], rowHeight - 4);
                            gfx.DrawString(childRow[i], smallFont, XBrushes.Black, rect, XStringFormats.Center);
                            childX += childColWidths[i];
                        }
                        y += rowHeight - 4;
                        // Draw row line for child
                        gfx.DrawLine(XPens.LightGray, margin + childIndent, y, margin + childIndent + childColWidths.Sum(), y);
                    }
                    // Add space after child rows
                    y += 6;
                }
            }

            // Draw totals row
            x = margin;
            string[] totals = new string[]
            {
                "TOTAL", "", "", "",
                totalVoided.TotalGross.ToString("N2"),
                totalVoided.TotalDiscount.ToString("N2"),
                totalVoided.TotalAmountDue.ToString("N2"),
                "", // NET OF SALES (if needed, calculate)
                totalVoided.TotalVatable.ToString("N2"),
                totalVoided.TotalVatZero.ToString("N2"),
                totalVoided.TotalExempt.ToString("N2"),
                "", "", "", "", ""
            };
            for (int i = 0; i < totals.Length; i++)
            {
                var rect = new XRect(x, y, colWidths[i], rowHeight);
                gfx.DrawRectangle(XBrushes.White, rect);
                gfx.DrawString(totals[i], totals[i] == "TOTAL" ? headerFont : smallFont, XBrushes.Black, rect, formats[i]);
                x += colWidths[i];
            }
            y += rowHeight;

            // Save PDF to byte array
            using (var stream = new MemoryStream())
            {
                document.Save(stream, false);
                return stream.ToArray();
            }
        }

    }
}
