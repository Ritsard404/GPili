#if WINDOWS
using ServiceLibrary.Services.DTO.Report;
using System.Globalization;
using System.Text;
using ServiceLibrary.Utils;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using QColors = QuestPDF.Helpers.Colors;
using QIContainer = QuestPDF.Infrastructure.IContainer;

namespace EBISX_POS.API.Services.PDF
{
    public class TransactionListPDFService
    {
        private string _businessName = "N/A";
        private string _address = "N/A";
        private string _tin = "N/A";

        public TransactionListPDFService() { }

        public void UpdateBusinessInfo(string businessName, string address, string tin)
        {
            _businessName = businessName;
            _address = address;
            _tin = tin;
        }

        public byte[] GenerateTransactionListPDF(List<TransactionListDTO> transactions, DateTime fromDate, DateTime toDate)
        {
            var columns = new[]
            {
                ("DATE", 0.07f),
                ("OR NO", 0.10f),
                ("SRC", 0.07f),
                ("DISC\nTYPE", 0.07f),
                ("%", 0.03f),
                ("SUB\nTOTAL", 0.06f),
                ("AMOUNT\nDUE", 0.06f),
                ("GROSS\nSALES", 0.06f),
                ("RETURNS", 0.06f),
                ("NET OF\nRETURNS", 0.06f),
                ("LESS\nDISCOUNT", 0.06f),
                ("NET OF\nSALES", 0.06f),
                ("VATABLE", 0.06f),
                ("ZERO\nRATED", 0.06f),
                ("EXEMPT", 0.06f),
                ("VAT", 0.06f)
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
                        headerCol.Item().Text("DAILY TRANSACTION LIST").Bold().FontSize(13).FontColor(QColors.Blue.Darken2);
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
                                    header.Cell().Element(CellStyle).Text(c.Item1.Replace("\n", "\n")).Bold().FontSize(10);
                            });

                            // Data rows
                            foreach (var t in transactions)
                            {
                                table.Cell().Element(CellStyle).Text(t.Date).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.InvoiceNum).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.Src).FontColor(t.Src == "REFUNDED" ? QColors.Red.Medium : QColors.Black).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.DiscType).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.Percent).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.SubTotal.PesoFormat()).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.AmountDue.PesoFormat()).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.GrossSales.PesoFormat()).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.Returns.PesoFormat()).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.NetOfReturns.PesoFormat()).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.LessDiscount.PesoFormat()).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.NetOfSales.PesoFormat()).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.Vatable.PesoFormat()).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.ZeroRated.PesoFormat()).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.Exempt.PesoFormat()).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.Vat.PesoFormat()).FontSize(10);
                            }

                            // Totals row
                            table.Cell().Element(CellStyle).Text(""); // DATE
                            table.Cell().Element(CellStyle).Text(""); // OR NO
                            table.Cell().Element(CellStyle).Text(""); // SRC
                            table.Cell().Element(CellStyle).Text(""); // DISC TYPE
                            table.Cell().Element(CellStyle).Text(""); // %
                            table.Cell().Element(CellStyle).Text(""); // SUB TOTAL
                            table.Cell().Element(CellStyle).Text("TOTALS:").Bold().FontSize(10);
                            table.Cell().Element(CellStyle).Text(transactions.Sum(t => t.GrossSales).PesoFormat()).FontSize(10);
                            table.Cell().Element(CellStyle).Text(transactions.Sum(t => t.Returns).PesoFormat()).FontSize(10);
                            table.Cell().Element(CellStyle).Text(transactions.Sum(t => t.NetOfReturns).PesoFormat()).FontSize(10);
                            table.Cell().Element(CellStyle).Text(transactions.Sum(t => t.LessDiscount).PesoFormat()).FontSize(10);
                            table.Cell().Element(CellStyle).Text(transactions.Sum(t => t.NetOfSales).PesoFormat()).FontSize(10);
                            table.Cell().Element(CellStyle).Text(transactions.Sum(t => t.Vatable).PesoFormat()).FontSize(10);
                            table.Cell().Element(CellStyle).Text(transactions.Sum(t => t.ZeroRated).PesoFormat()).FontSize(10);
                            table.Cell().Element(CellStyle).Text(transactions.Sum(t => t.Exempt).PesoFormat()).FontSize(10);
                            table.Cell().Element(CellStyle).Text(transactions.Sum(t => t.Vat).PesoFormat()).FontSize(10);
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

        public byte[] GeneratePwdOrSeniorListPDF(List<TransactionListDTO> transactions, DateTime fromDate, DateTime toDate, string type)
        {
            var phCulture = new CultureInfo("en-PH");
            var columns = new[]
            {
                ("DATE", 0.07f),
                ("OR NO", 0.10f),
                ("SRC", 0.07f),
                ("DISC\nTYPE", 0.07f),
                ("%", 0.03f),
                ("SUB\nTOTAL", 0.06f),
                ("AMOUNT\nDUE", 0.06f),
                ("GROSS\nSALES", 0.06f),
                ("RETURNS", 0.06f),
                ("NET OF\nRETURNS", 0.06f),
                ("LESS\nDISCOUNT", 0.06f),
                ("NET OF\nSALES", 0.06f),
                ("VATABLE", 0.06f),
                ("ZERO\nRATED", 0.06f),
                ("EXEMPT", 0.06f),
                ("VAT", 0.06f)
            };
            var title = type.ToUpper() == "PWD" ? "PERSON WITH DISABILITY LIST" : "SENIOR CITIZEN LIST";

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
                        headerCol.Item().Text(title).Bold().FontSize(13).FontColor(QColors.Blue.Darken2);
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
                                    header.Cell().Element(CellStyle).Text(c.Item1.Replace("\n", "\n")).Bold();
                            });

                            // Data rows
                            foreach (var t in transactions)
                            {
                                table.Cell().Element(CellStyle).Text(t.Date).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.InvoiceNum).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.Src).FontColor(t.Src == "REFUNDED" ? QColors.Red.Medium : QColors.Black).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.DiscType).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.Percent).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.SubTotal.PesoFormat()).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.AmountDue.PesoFormat()).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.GrossSales.PesoFormat()).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.Returns.PesoFormat()).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.NetOfReturns.PesoFormat()).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.LessDiscount.PesoFormat()).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.NetOfSales.PesoFormat()).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.Vatable.PesoFormat()).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.ZeroRated.PesoFormat()).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.Exempt.PesoFormat()).FontSize(10);
                                table.Cell().Element(CellStyle).Text(t.Vat.PesoFormat()).FontSize(10);
                            }

                            // Totals row
                            table.Cell().Element(CellStyle).Text(""); // DATE
                            table.Cell().Element(CellStyle).Text(""); // OR NO
                            table.Cell().Element(CellStyle).Text(""); // SRC
                            table.Cell().Element(CellStyle).Text(""); // DISC TYPE
                            table.Cell().Element(CellStyle).Text(""); // %
                            table.Cell().Element(CellStyle).Text(""); // SUB TOTAL
                            table.Cell().Element(CellStyle).Text("TOTALS:").Bold().FontSize(10);
                            table.Cell().Element(CellStyle).Text(transactions.Sum(t => t.GrossSales).PesoFormat()).FontSize(10);
                            table.Cell().Element(CellStyle).Text(transactions.Sum(t => t.Returns).PesoFormat()).FontSize(10);
                            table.Cell().Element(CellStyle).Text(transactions.Sum(t => t.NetOfReturns).PesoFormat()).FontSize(10);
                            table.Cell().Element(CellStyle).Text(transactions.Sum(t => t.LessDiscount).PesoFormat()).FontSize(10);
                            table.Cell().Element(CellStyle).Text(transactions.Sum(t => t.NetOfSales).PesoFormat()).FontSize(10);
                            table.Cell().Element(CellStyle).Text(transactions.Sum(t => t.Vatable).PesoFormat()).FontSize(10);
                            table.Cell().Element(CellStyle).Text(transactions.Sum(t => t.ZeroRated).PesoFormat()).FontSize(10);
                            table.Cell().Element(CellStyle).Text(transactions.Sum(t => t.Exempt).PesoFormat()).FontSize(10);
                            table.Cell().Element(CellStyle).Text(transactions.Sum(t => t.Vat).PesoFormat()).FontSize(10);
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
    }
}
#else
using ServiceLibrary.Services.DTO.Report;
using System;
using System.Collections.Generic;

namespace EBISX_POS.API.Services.PDF
{
    public class TransactionListPDFService
    {
        public TransactionListPDFService() { }
        public void UpdateBusinessInfo(string businessName, string address, string tin) { }
        public byte[] GenerateTransactionListPDF(List<TransactionListDTO> transactions, DateTime fromDate, DateTime toDate)
        {
            throw new NotSupportedException("PDF generation is only supported on Windows.");
        }
        public byte[] GeneratePwdOrSeniorListPDF(List<TransactionListDTO> transactions, DateTime fromDate, DateTime toDate, string type)
        {
            throw new NotSupportedException("PDF generation is only supported on Windows.");
        }
    }
}
#endif
