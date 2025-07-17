using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using ServiceLibrary.Services.DTO.Report;
using ServiceLibrary.Services.Interfaces;
using System.Globalization;
using System.Text;
using QColors = QuestPDF.Helpers.Colors;
using QIContainer = QuestPDF.Infrastructure.IContainer;

namespace ServiceLibrary.Services.PDF
{
    public class VoidedListPDFService(IGPiliTerminalMachine _terminalMachine)
    {
        public async Task<byte[]> GenerateVoidedListPDF(List<VoidedListDTO> voidedLists, TotalVoidedListDTO totalVoided, DateTime fromDate, DateTime toDate)
        {
            var posInfo = await _terminalMachine.GetTerminalInfo();
            var phCulture = new CultureInfo("en-PH");
            var columns = new[]
            {
                ("DATE", 0.06f),
                ("OR NO", 0.09f),
                ("DISC\nTYPE", 0.06f),
                ("%", 0.03f),
                ("GROSS\nSALES", 0.06f),
                ("DISCOUNT", 0.06f),
                ("AMOUNT\nDUE", 0.06f),
                ("NET OF\nSALES", 0.06f),
                ("VATABLE", 0.05f),
                ("ZERO\nRATED", 0.04f),
                ("EXEMPT", 0.04f),
                ("REASON", 0.12f),
                ("USER", 0.09f),
                ("CANCELLED\nBY", 0.09f),
                ("CANCELLED\nDATE", 0.06f),
                ("CANCELLED\nTIME", 0.06f)
            };

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(936, 612); // 13 x 8.5 inches, landscape (long bond)
                    page.Margin(30);

                    // HEADER
                    page.Header().Column(headerCol =>
                    {
                        headerCol.Item().Text(posInfo.RegisteredName).Bold().FontSize(16).FontColor(QColors.Blue.Darken2);
                        headerCol.Item().Text(posInfo.Address).FontSize(10);
                        headerCol.Item().Text($"TIN {posInfo.VatTinNumber}").FontSize(10);
                        headerCol.Item().PaddingVertical(10).LineHorizontal(1);
                        headerCol.Item().Text("VOIDED LIST REPORT").Bold().FontSize(13).FontColor(QColors.Blue.Darken2);
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

                            // Data rows (parent and child)
                            foreach (var item in voidedLists)
                            {
                                // Parent row
                                table.Cell().Element(CellStyle).Text(item.Date).FontSize(10);
                                table.Cell().Element(CellStyle).Text(item.InvoiceNum).FontSize(10);
                                table.Cell().Element(CellStyle).Text(item.DiscType).FontSize(10);
                                table.Cell().Element(CellStyle).Text(item.Percent).FontSize(10);
                                table.Cell().Element(CellStyle).Text(item.GrossSales.ToString("N2", phCulture)).FontSize(10);
                                table.Cell().Element(CellStyle).Text(item.Discount.ToString("N2", phCulture)).FontSize(10);
                                table.Cell().Element(CellStyle).Text(item.AmountDue.ToString("N2", phCulture)).FontSize(10);
                                table.Cell().Element(CellStyle).Text((item.AmountDue - item.Discount).ToString("N2", phCulture)).FontSize(10);
                                table.Cell().Element(CellStyle).Text(item.Vatable.ToString("N2", phCulture)).FontSize(10);
                                table.Cell().Element(CellStyle).Text(item.ZeroRated.ToString("N2", phCulture)).FontSize(10);
                                table.Cell().Element(CellStyle).Text(item.Exempt.ToString("N2", phCulture)).FontSize(10);
                                table.Cell().Element(CellStyle).Text(item.Reason ?? string.Empty).FontSize(10);
                                table.Cell().Element(CellStyle).Text(item.User).FontSize(10);
                                table.Cell().Element(CellStyle).Text(item.CancelledBy).FontSize(10);
                                table.Cell().Element(CellStyle).Text(item.CancelledDate).FontSize(10);
                                table.Cell().Element(CellStyle).Text(item.CancelledTime).FontSize(10);

                                // Child rows (VoidedItemList)
                                if (item.VoidedItemList != null && item.VoidedItemList.Count > 0)
                                {
                                    table.Cell().ColumnSpan(16).Element(childContainer =>
                                    {
                                        childContainer.PaddingLeft(30).Table(childTable =>
                                        {
                                            var childHeaders = new[] { "No", "Barcode", "Item Name", "Qty", "Price", "Amount", "Return", "Reason" };
                                            var childColWidths = new[] { 40f, 80f, 200f, 60f, 60f, 70f, 70f, 120f };
                                            // Define columns
                                            childTable.ColumnsDefinition(def =>
                                            {
                                                foreach (var w in childColWidths)
                                                    def.ConstantColumn(w);
                                            });
                                            // Header
                                            childTable.Header(header =>
                                            {
                                                foreach (var h in childHeaders)
                                                    header.Cell().Element(CellStyleChild).Text(h).Bold();
                                            });
                                            // Rows
                                            foreach (var child in item.VoidedItemList)
                                            {
                                                childTable.Cell().Element(CellStyleChild).Text(child.No.ToString()).FontSize(10);
                                                childTable.Cell().Element(CellStyleChild).Text(child.Barcode).FontSize(10);
                                                childTable.Cell().Element(CellStyleChild).Text(child.ItemName).FontSize(10);
                                                childTable.Cell().Element(CellStyleChild).Text(child.Quantity.ToString("N2", phCulture)).FontSize(10);
                                                childTable.Cell().Element(CellStyleChild).Text(child.Price.ToString("N2", phCulture)).FontSize(10);
                                                childTable.Cell().Element(CellStyleChild).Text(child.Amount.ToString("N2", phCulture)).FontSize(10);
                                                childTable.Cell().Element(CellStyleChild).Text(child.Return.ToString("N2", phCulture)).FontSize(10);
                                                childTable.Cell().Element(CellStyleChild).Text(child.Reason ?? string.Empty).FontSize(10);
                                            }
                                        });
                                    });
                                }
                            }

                            // Totals row
                            table.Cell().Element(CellStyle).Text("TOTAL").Bold().FontSize(10);
                            table.Cell().Element(CellStyle).Text("");
                            table.Cell().Element(CellStyle).Text("");
                            table.Cell().Element(CellStyle).Text("");
                            table.Cell().Element(CellStyle).Text(totalVoided.TotalGross.ToString("N2", phCulture)).FontSize(10);
                            table.Cell().Element(CellStyle).Text(totalVoided.TotalDiscount.ToString("N2", phCulture)).FontSize(10);
                            table.Cell().Element(CellStyle).Text(totalVoided.TotalAmountDue.ToString("N2", phCulture)).FontSize(10);
                            table.Cell().Element(CellStyle).Text(""); // NET OF SALES (if needed, calculate)
                            table.Cell().Element(CellStyle).Text(totalVoided.TotalVatable.ToString("N2", phCulture)).FontSize(10);
                            table.Cell().Element(CellStyle).Text(totalVoided.TotalVatZero.ToString("N2", phCulture)).FontSize(10);
                            table.Cell().Element(CellStyle).Text(totalVoided.TotalExempt.ToString("N2", phCulture)).FontSize(10);
                            table.Cell().Element(CellStyle).Text("");
                            table.Cell().Element(CellStyle).Text("");
                            table.Cell().Element(CellStyle).Text("");
                            table.Cell().Element(CellStyle).Text("");
                            table.Cell().Element(CellStyle).Text("");
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
        private static QIContainer CellStyleChild(QIContainer container)
        {
            return container
                .BorderBottom(0.5f)
                .BorderColor(QColors.Grey.Lighten3)
                .PaddingVertical(1)
                .PaddingHorizontal(2)
                .AlignMiddle();
        }
    }
}
