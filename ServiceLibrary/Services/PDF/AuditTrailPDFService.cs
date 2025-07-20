#if WINDOWS
using ServiceLibrary.Services.DTO.Report;
using QuestPDF.Fluent;
using QColors = QuestPDF.Helpers.Colors;
using QIContainer = QuestPDF.Infrastructure.IContainer;

namespace ServiceLibrary.Services.PDF
{
    public class AuditTrailPDFService
    {
        private string _businessName = "N/A";
        private string _address = "N/A";
        private string _vatTinNumber = "N/A";
        private string _minNumber = "N/A";
        private string _serialNumber = "N/A";

        public void UpdateBusinessInfo(
            string businessName,
            string address,
            string vatTinNumber,
            string minNumber,
            string serialNumber)
        {
            _businessName = businessName;
            _address = address;
            _vatTinNumber = vatTinNumber;
            _minNumber = minNumber;
            _serialNumber = serialNumber;
        }

        public byte[] GenerateAuditTrailPDF(
            List<AuditTrailDTO> auditTrail,
            DateTime fromDate,
            DateTime toDate)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    // HEADER
                    page.Header().Column(col =>
                    {
                        col.Item().Text(_businessName).Bold().FontSize(16);
                        col.Item().Text(_address).FontSize(10);
                        col.Item().Text($"VAT TIN: {_vatTinNumber}").FontSize(10);
                        col.Item().Text($"MIN: {_minNumber}").FontSize(10);
                        col.Item().Text($"S/N: {_serialNumber}").FontSize(10);
                        col.Item().PaddingVertical(10).LineHorizontal(1);
                    });

                    // CONTENT
                    page.Content().Column(col =>
                    {
                        col.Item()
                            .Text("AUDIT TRAIL REPORT")
                            .Bold()
                            .FontSize(14)
                            .FontColor(QColors.Blue.Darken2);

                        col.Item()
                            .Text($"From: {fromDate:MM/dd/yyyy}  To: {toDate:MM/dd/yyyy}")
                            .FontSize(10);

                        col.Item()
                            .Text($"Generated: {DateTime.Now:MM/dd/yyyy hh:mm tt}")
                            .FontSize(10);

                        col.Item().PaddingVertical(10);

                        col.Item().Table(table =>
                        {
                            // define 5 columns (Date, Time, User, Action, Amount)
                            table.ColumnsDefinition(def =>
                            {
                                def.RelativeColumn(1);
                                def.RelativeColumn(1);
                                def.RelativeColumn(2);
                                def.RelativeColumn(3);
                                def.RelativeColumn(1);
                            });

                            // header row
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Date").Bold();
                                header.Cell().Element(CellStyle).Text("Time").Bold();
                                header.Cell().Element(CellStyle).Text("User").Bold();
                                header.Cell().Element(CellStyle).Text("Action").Bold();
                                header.Cell().Element(CellStyle).Text("Amount").Bold();
                            });

                            // data rows
                            foreach (var entry in auditTrail)
                            {
                                table.Cell().Element(CellStyle)
                                    .Text(entry.Date).FontSize(11);
                                table.Cell().Element(CellStyle)
                                    .Text(entry.Time).FontSize(11);
                                table.Cell().Element(CellStyle)
                                    .Text(entry.UserName).FontSize(11);
                                table.Cell().Element(CellStyle)
                                    .Text(entry.Action).FontSize(11);
                                table.Cell().Element(CellStyle)
                                    .Text(entry.Amount ?? "-").FontSize(11);
                            }
                        });
                    });

                    // FOOTER
                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("EBISX POS System").FontSize(8);
                            x.Span(" | ");
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

        // Alias to QuestPDF.IContainer, unambiguously
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

namespace ServiceLibrary.Services.PDF
{
    public class AuditTrailPDFService
    {
        public void UpdateBusinessInfo(string businessName, string address, string vatTinNumber, string minNumber, string serialNumber) { }
        public byte[] GenerateAuditTrailPDF(List<AuditTrailDTO> auditTrail, DateTime fromDate, DateTime toDate)
        {
            throw new NotSupportedException("PDF generation is only supported on Windows.");
        }
    }
}
#endif
