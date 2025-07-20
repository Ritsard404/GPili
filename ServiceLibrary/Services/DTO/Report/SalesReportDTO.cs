namespace ServiceLibrary.Services.DTO.Report
{
    public class SalesReportDTO
    {
        public DateTime InvoiceDate { get; set; }
        public long InvoiceNumber { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string BaseUnit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Cost { get; set; }
        public decimal Price { get; set; }
        public string ItemGroup { get; set; } = string.Empty;
        public string Barcode { get; set; }
        public bool IsReturned { get; set; }
        public DateTimeOffset? ReturnDate { get; set; }
        public decimal ReturnAmount { get; set; }

        // Adjust calculations based on return status
        public decimal TotalCost => Cost * Quantity;
        public decimal Revenue => IsReturned ? -ReturnAmount : Price * Quantity;
        public decimal Profit => Revenue - TotalCost;

        // Helper property for display
        public string Status => IsReturned ? $"RETURNED ({ReturnDate:MM/dd/yyyy})" : "SOLD";
        public string StatusColor => IsReturned ? "Red" : "Black";

    }
    public class TotalSalesReportDTO
    {
        public string TotalRevenue { get; set; } = string.Empty;
        public string TotalCost { get; set; } = string.Empty;
        public string TotalProfit { get; set; } = string.Empty;
        public string TotalPrice { get; set; } = string.Empty;
        public string OverallTotalCost { get; set; } = string.Empty;
    }
}
