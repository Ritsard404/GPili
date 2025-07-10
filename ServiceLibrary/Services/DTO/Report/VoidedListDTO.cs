namespace ServiceLibrary.Services.DTO.Report
{
    public class VoidedListDTO
    {
        public string Date { get; set; }
        public string InvoiceNum { get; set; }
        public string DiscType { get; set; }
        public string Percent { get; set; }
        public decimal GrossSales { get; set; }
        public decimal Discount { get; set; }
        public decimal AmountDue { get; set; }
        public decimal Vatable { get; set; }
        public decimal ZeroRated { get; set; }
        public decimal Exempt { get; set; }
        public decimal Vat { get; set; }
        public string? Reason { get; set; }
        public string User { get; set; }
        public string CancelledBy { get; set; }
        public string CancelledDate { get; set; }
        public string CancelledTime { get; set; }
        public List<VoidedItemListDTO> VoidedItemList { get; set; } 
    }
    public class TotalVoidedListDTO
    {
        public decimal TotalGross { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal TotalAmountDue { get; set; }
        public decimal TotalVatable { get; set; }
        public decimal TotalVatZero { get; set; }
        public decimal TotalExempt { get; set; }
        public decimal TotalVat { get; set; }
    }
    public class VoidedItemListDTO
    {
        public long No { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
        public decimal Return { get; set; }
        public string? Reason { get; set; }
        public decimal ReturnQuantity { get; set; }
    }

}
