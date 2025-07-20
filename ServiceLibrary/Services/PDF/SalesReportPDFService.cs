#if WINDOWS
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using ServiceLibrary.Models;
using ServiceLibrary.Services.DTO.Report;
using System.Globalization;
using System.Text;
using QColors = QuestPDF.Helpers.Colors;
using QIContainer = QuestPDF.Infrastructure.IContainer;
using ServiceLibrary.Utils;

namespace EBISX_POS.API.Services.PDF
{
    public class SalesReportPDFService
    {
        private string _businessName = "N/A";
        private string _address = "N/A";
        private string _tin = "N/A";

        public SalesReportPDFService() { }

        public void UpdateBusinessInfo(string businessName, string address, string tin)
        {
            _businessName = businessName;
            _address = address;
            _tin = tin;
        }

        public byte[] GenerateSalesReportPDF(List<SalesReportDTO> sales, DateTime fromDate, DateTime toDate)
        {
            var columns = new[]
            {
                ("DATE", 0.065f),
                ("INVOICE", 0.073f),
                ("ITEM NAME", 0.174f),
                ("UNIT", 0.046f),
                ("QTY", 0.037f),
                ("COST", 0.046f),
                ("PRICE", 0.064f),
                ("GROUP", 0.138f),
                ("BARCODE", 0.092f),
                ("STATUS", 0.073f),
                ("TOTAL COST", 0.064f),
                ("REVENUE", 0.064f),
                ("PROFIT", 0.064f)
            };

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Legal.Landscape());
                    page.Margin(30);

                    // HEADER
                    page.Header().Column(headerCol =>
                    {
                        headerCol.Item().Text(_businessName).Bold().FontSize(16).FontColor(QColors.Blue.Darken2);
                        headerCol.Item().Text(_address).FontSize(10);
                        headerCol.Item().Text($"TIN {_tin}").FontSize(10);
                        headerCol.Item().PaddingVertical(10).LineHorizontal(1);
                        headerCol.Item().Text("SALES REPORT").Bold().FontSize(13).FontColor(QColors.Blue.Darken2);
                        headerCol.Item().Text($"From {fromDate:MM-dd-yyyy} To {toDate:MM-dd-yyyy}").FontSize(10);
                    });

                    // CONTENT
                    page.Content().Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            // Define columns
                            table.ColumnsDefinition(def =>
                            {
                                foreach (var c in columns)
                                    def.RelativeColumn(c.Item2);
                            });

                            // Header row
                            table.Header(header =>
                            {
                                foreach (var c in columns)
                                    header.Cell().Element(CellStyle).Text(c.Item1).Bold().FontSize(11);
                            });

                            // Data rows
                            foreach (var sale in sales)
                            {
                                table.Cell().Element(CellStyle).Text(sale.InvoiceDate.DateFormat()).FontSize(11);
                                table.Cell().Element(CellStyle).Text(sale.InvoiceNumber.ToString()).FontSize(11).AlignCenter();
                                table.Cell().Element(CellStyle).Text(TruncateWithEllipsis(sale.ItemName, 30)).FontSize(11);
                                table.Cell().Element(CellStyle).Text(sale.BaseUnit).FontSize(11);
                                table.Cell().Element(CellStyle).Text(sale.Quantity.ToString()).FontSize(11);
                                table.Cell().Element(CellStyle).Text(sale.Cost.PesoFormat()).FontSize(11);
                                table.Cell().Element(CellStyle).Text(sale.Price.PesoFormat()).FontSize(11);
                                table.Cell().Element(CellStyle).Text(TruncateWithEllipsis(sale.ItemGroup, 25)).FontSize(11);
                                table.Cell().Element(CellStyle).Text(sale.Barcode).FontSize(10);
                                table.Cell().Element(CellStyle).Text(sale.Status).FontColor(sale.IsReturned ? QColors.Red.Medium : QColors.Black).FontSize(11);
                                table.Cell().Element(CellStyle).Text(sale.TotalCost.PesoFormat()).FontSize(11);
                                table.Cell().Element(CellStyle).Text(sale.Revenue.PesoFormat()).FontSize(11);
                                table.Cell().Element(CellStyle).Text(sale.Profit.PesoFormat()).FontSize(11);
                            }

                            // Totals row
                            var totalCost = sales.Where(s => !s.IsReturned).Sum(s => s.Cost);
                            var totalPrice = sales.Where(s => !s.IsReturned).Sum(s => s.Price);
                            var totalTotalCost = sales.Where(s => !s.IsReturned).Sum(s => s.TotalCost);
                            var totalRevenue = sales.Where(s => !s.IsReturned).Sum(s => s.Revenue);
                            var totalProfit = sales.Where(s => !s.IsReturned).Sum(s => s.Profit);

                            table.Cell().Element(CellStyle).Text(""); // DATE
                            table.Cell().Element(CellStyle).Text(""); // INVOICE
                            table.Cell().Element(CellStyle).Text("TOTALS:").Bold();
                            table.Cell().Element(CellStyle).Text(""); // UNIT
                            table.Cell().Element(CellStyle).Text(""); // QTY
                            table.Cell().Element(CellStyle).Text(totalCost.PesoFormat());
                            table.Cell().Element(CellStyle).Text(totalPrice.PesoFormat());
                            table.Cell().Element(CellStyle).Text(""); // GROUP
                            table.Cell().Element(CellStyle).Text(""); // BARCODE
                            table.Cell().Element(CellStyle).Text(""); // STATUS
                            table.Cell().Element(CellStyle).Text(totalTotalCost.PesoFormat());
                            table.Cell().Element(CellStyle).Text(totalRevenue.PesoFormat());
                            table.Cell().Element(CellStyle).Text(totalProfit.PesoFormat());
                        });
                    });

                    // FOOTER
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("EBISX POS System").FontSize(8);
                        x.Span(" | Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            });

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        public byte[] GenerateSalesBookPDF(List<Reading> readings, DateTime fromDate, DateTime toDate)
        {
            var phCulture = new CultureInfo("en-PH");
            var columns = new[]
            {
                ("DATE", 0.15f),
                ("INVOICE", 0.18f),
                ("PREVIOUS", 0.18f),
                ("PRESENT", 0.18f),
                ("SALES", 0.16f),
                ("Z-COUNTER", 0.15f)
            };

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);

                    // HEADER
                    page.Header().Column(headerCol =>
                    {
                        headerCol.Item().Text(_businessName).Bold().FontSize(16).FontColor(QColors.Blue.Darken2);
                        headerCol.Item().Text(_address).FontSize(10);
                        headerCol.Item().Text($"TIN {_tin}").FontSize(10);
                        headerCol.Item().PaddingVertical(10).LineHorizontal(1);
                        headerCol.Item().Text("SALES BOOK REPORT").Bold().FontSize(13).FontColor(QColors.Blue.Darken2);
                        headerCol.Item().Text($"From {fromDate:MM-dd-yyyy} To {toDate:MM-dd-yyyy}").FontSize(10);
                    });

                    // CONTENT
                    page.Content().Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            // Define columns
                            table.ColumnsDefinition(def =>
                            {
                                foreach (var c in columns)
                                    def.RelativeColumn(c.Item2);
                            });

                            // Header row
                            table.Header(header =>
                            {
                                foreach (var c in columns)
                                    header.Cell().Element(CellStyle).Text(c.Item1).Bold();
                            });

                            // Data rows
                            foreach (var reading in readings)
                            {
                                table.Cell().Element(CellStyle).Text(reading.CreatedAt.ToString("MM/dd/yyyy", phCulture));
                                table.Cell().Element(CellStyle).Text(reading.LastInvoice);
                                table.Cell().Element(CellStyle).Text(reading.Previous.PesoFormat());
                                table.Cell().Element(CellStyle).Text(reading.Present.PesoFormat());
                                table.Cell().Element(CellStyle).Text(reading.Sales.PesoFormat());
                                table.Cell().Element(CellStyle).Text(reading.Id.ToString());
                            }
                        });
                    });

                    // FOOTER
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("EBISX POS System").FontSize(8);
                        x.Span(" | Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            });

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        private static QIContainer CellStyle(QIContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(QColors.Grey.Lighten2)
                .PaddingVertical(2)
                .PaddingHorizontal(4)
                .AlignMiddle();
        }

        private string TruncateWithEllipsis(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Length <= maxLength ? text : text.Substring(0, maxLength - 3) + "...";
        }
    }
}
#else
using ServiceLibrary.Models;
using ServiceLibrary.Services.DTO.Report;
using System;
using System.Collections.Generic;

namespace EBISX_POS.API.Services.PDF
{
    public class SalesReportPDFService
    {
        public SalesReportPDFService() { }
        public void UpdateBusinessInfo(string businessName, string address, string tin) { }
        public byte[] GenerateSalesReportPDF(List<SalesReportDTO> sales, DateTime fromDate, DateTime toDate)
        {
            throw new NotSupportedException("PDF generation is only supported on Windows.");
        }
        public byte[] GenerateSalesBookPDF(List<Reading> readings, DateTime fromDate, DateTime toDate)
        {
            throw new NotSupportedException("PDF generation is only supported on Windows.");
        }
    }
}
#endif