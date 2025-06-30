using System.ComponentModel.DataAnnotations;

namespace ServiceLibrary.Models
{
    public class Inventory
    {
        [Key]
        public long Id { get; set; }
        public decimal Quantity { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public required string Type { get; set; } // "IN" or "OUT"
        public string? Reference { get; set; } // e.g., Invoice number, reason, etc.

        public required Product Product { get; set; }
    }
}
