﻿using System.ComponentModel.DataAnnotations;

namespace ServiceLibrary.Models
{
    public class Invoice
    {
        [Key]
        public long Id { get; set; }
        public required long InvoiceNumber { get; set; }
        public required decimal GrossAmount { get; set; }
        public required decimal TotalAmount { get; set; }
        public decimal? SubTotal { get; set; }
        public decimal? CashTendered { get; set; }
        public decimal? DueAmount { get; set; }
        public decimal? TotalTendered { get; set; }
        public decimal? ChangeAmount { get; set; }
        public decimal? VatSales { get; set; }
        public decimal? VatExempt { get; set; }
        public decimal? VatAmount { get; set; }
        public decimal? VatZero { get; set; }

        public string CustomerName { get; set; } = "Walk-in Customer";
        public string? EligibleDiscName { get; set; }
        public string? OSCAIdNum { get; set; }
        public string? DiscountType { get; set; }
        public int? DiscountPercent { get; set; }
        public decimal? DiscountAmount { get; set; }

        // Return Amount
        public decimal? ReturnedAmount { get; set; }
        public string? Reason { get; set; }
        public User? VoidedBy { get; set; }

        public required User Cashier { get; set; }
        public ICollection<Item> Items { get; set; } = new List<Item>();
        public ICollection<EPayment> EPayments { get; set; } = new List<EPayment>();


        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? StatusChangeDate { get; set; }
        public required string Status { get; set; }
        public bool IsRead { get; set; } = false;
        public bool IsTrainMode { get; set; } = false;
    }
}
