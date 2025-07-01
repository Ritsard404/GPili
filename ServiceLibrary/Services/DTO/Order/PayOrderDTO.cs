using ServiceLibrary.Models;
using ServiceLibrary.Services.DTO.Payment;

namespace ServiceLibrary.Services.DTO.Order
{
    public class PayOrderDTO
    {
        public required decimal TotalAmount { get; set; }
        public required decimal SubTotal { get; set; }
        public required decimal CashTendered { get; set; }
        public required List<EPaymentDTO> OtherPayment { get; set; }
        public DiscountDTO? Discount { get; set; }
        public required decimal ChangeAmount { get; set; }
        public required decimal DueAmount { get; set; }
        public required decimal TotalTendered { get; set; }
        public required decimal DiscountAmount { get; set; }
        public required decimal VatExempt { get; set; }
        public required decimal VatSales { get; set; }
        public required decimal VatAmount { get; set; }
        public required decimal VatZero { get; set; }

        public required string CashierEmail { get; set; }
    }

    public class DiscountDTO
    {
        public string? EligibleDiscName { get; set; }
        public string? OSCAIdNum { get; set; }
        public string? DiscountType { get; set; }
        public int? DiscountPercent { get; set; }
        public decimal? DiscountAmount { get; set; }
    }
}
