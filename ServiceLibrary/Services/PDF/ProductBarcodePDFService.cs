#if WINDOWS
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using ZXing;
using ZXing.Common;
using ServiceLibrary.Models;
using QColors = QuestPDF.Helpers.Colors;
using QIContainer = QuestPDF.Infrastructure.IContainer;
using SkiaSharp;
using ZXing.SkiaSharp;
using System.Runtime.InteropServices;

namespace ServiceLibrary.Services.PDF
{
    public class ProductBarcodePDFService
    {
        // Long bond paper: 8.5 x 13 inches = 612 x 936 points
        private const float PAGE_WIDTH = 612f;
        private const float PAGE_HEIGHT = 936f;
        private const float MARGIN = 30f;
        private const float INNER_PADDING = 5f;
        private const float LABEL_WIDTH = 175f;
        private const float LABEL_HEIGHT = 70f;
        private const float BARCODE_HEIGHT = 25f;
        private const float TEXT_SPACING = 5f;
        private const float CATEGORY_HEADER_HEIGHT = 40f;
        private const float CATEGORY_SPACING = 25f;
        private const float CATEGORY_PADDING = 10f;

        private const string FONT_FAMILY = "Arial";
        private const float ID_FONT_SIZE = 12f;
        private const float NAME_FONT_SIZE = 10f;
        private const float CATEGORY_FONT_SIZE = 14f;

        private readonly BarcodeWriterPixelData _barcodeWriter;

        public ProductBarcodePDFService()
        {
            _barcodeWriter = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Width = (int)LABEL_WIDTH,
                    Height = (int)BARCODE_HEIGHT,
                    Margin = 0,
                    PureBarcode = true
                },
            };
        }

        public byte[] GenerateProductBarcodeLabels(List<Product> products)
        {
            var productByCategory = products
                .Where(m => m.Category != null)
                .GroupBy(m => m.Category.CtgryName)
                .OrderBy(g => g.Key)
                .ToList();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    //page.Size(PAGE_WIDTH, PAGE_HEIGHT);
                    page.Margin(MARGIN);

                    // CONTENT
                    page.Content().Column(mainCol =>
                    {
                        foreach (var categoryGroup in productByCategory)
                        {
                            var categoryName = categoryGroup.Key;
                            var categoryProducts = categoryGroup.ToList();

                            // Category Header
                            mainCol.Item().Element(c => DrawCategoryHeader(c, categoryName));
                            mainCol.Item().PaddingBottom(CATEGORY_SPACING / 2);

                            // Product Labels Grid
                            mainCol.Item().Element(c => DrawProductLabelsGrid(c, categoryProducts));

                            mainCol.Item().PaddingBottom(CATEGORY_SPACING);
                        }
                    });

                    // FOOTER
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("GPili Product Barcode Labels").FontSize(8);
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

        private void DrawCategoryHeader(QIContainer container, string categoryName)
        {
            container
                .Background(QColors.Blue.Lighten4)
                .Padding(CATEGORY_PADDING)
                .Height(CATEGORY_HEADER_HEIGHT)
                .AlignMiddle()
                .Row(row =>
                {
                    row.RelativeItem().Text(categoryName.Length > 40 ? categoryName.Substring(0, 37) + "..." : categoryName)
                        .FontFamily(FONT_FAMILY)
                        .FontSize(CATEGORY_FONT_SIZE)
                        .Bold()
                        .FontColor(QColors.Blue.Darken2);
                });
        }

        private void DrawProductLabelsGrid(QIContainer container, List<Product> products)
        {
            // Calculate columns per row for page width minus margins
            int columnsPerRow = (int)((PAGE_WIDTH - (MARGIN * 2)) / LABEL_WIDTH);
            if (columnsPerRow < 1) columnsPerRow = 1;

            container.Column(col =>
            {
                for (int i = 0; i < products.Count; i += columnsPerRow)
                {
                    var rowProducts = products.Skip(i).Take(columnsPerRow).ToList();
                    col.Item().Row(row =>
                    {
                        foreach (var product in rowProducts)
                        {
                            row.RelativeItem(1).Width(LABEL_WIDTH).Height(LABEL_HEIGHT).Element(c => DrawLabel(c, product));
                        }
                        // Fill empty columns if last row is not full
                        int empty = columnsPerRow - rowProducts.Count;
                        for (int j = 0; j < empty; j++)
                        {
                            row.RelativeItem(1).Width(LABEL_WIDTH).Height(LABEL_HEIGHT);
                        }
                    });
                    col.Item().PaddingBottom(5);
                }
            });
        }

        private void DrawLabel(QIContainer container, Product product)
        {
            container
                .Background(QColors.White)
                .Border(0.5f)
                .BorderColor(QColors.Grey.Lighten2)
                .Padding(INNER_PADDING)
                .Column(col =>
                {
                    // Barcode image (centered horizontally)
                    col.Item().AlignCenter().Element(c =>
                    {
                        var barcodeBytes = GenerateBarcodeBytes(product.Barcode);
                        if (barcodeBytes != null)
                        {
                            using var imgStream = new MemoryStream(barcodeBytes);
                            c.Image(imgStream)
                                .FitWidth();
                        }
                    });

                    // Product barcode (centered, bold)
                    col.Item().PaddingTop(TEXT_SPACING).AlignCenter().Text(product.Barcode)
                        .FontFamily(FONT_FAMILY)
                        .FontSize(ID_FONT_SIZE)
                        .Bold();

                    // Product name (centered, smaller font)
                    col.Item().PaddingTop(2).AlignCenter().Text(
                        product.Name.Length > 20 ? product.Name.Substring(0, 17) + "..." : product.Name)
                        .FontFamily(FONT_FAMILY)
                        .FontSize(NAME_FONT_SIZE);
                });
        }

        private byte[]? GenerateBarcodeBytes(string text)
        {
            try
            {
                var pixelData = _barcodeWriter.Write(text);
                var info = new SKImageInfo(pixelData.Width, pixelData.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
                using var skBitmap = new SKBitmap(info);

                // Copy pixel data into the bitmap safely
                Marshal.Copy(pixelData.Pixels, 0, skBitmap.GetPixels(), pixelData.Pixels.Length);

                using var image = SKImage.FromBitmap(skBitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                return data.ToArray();
            }
            catch
            {
                return null;
            }
        }
    }
}
#else
using ServiceLibrary.Models;
using System;
using System.Collections.Generic;

namespace ServiceLibrary.Services.PDF
{
    public class ProductBarcodePDFService
    {
        public ProductBarcodePDFService() { }
        public byte[] GenerateProductBarcodeLabels(List<Product> products)
        {
            throw new NotSupportedException("PDF generation is only supported on Windows.");
        }
    }
}
#endif