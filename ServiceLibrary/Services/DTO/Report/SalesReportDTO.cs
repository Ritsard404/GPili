﻿namespace ServiceLibrary.Services.DTO.Report
{
    public class SalesReportDTO
    {
        public DateTimeOffset InvoiceDate { get; set; }
        public long InvoiceNumber { get; set; }
        public string MenuName { get; set; } = string.Empty;
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
    }
 }
