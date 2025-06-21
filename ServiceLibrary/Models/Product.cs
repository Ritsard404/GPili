using System.ComponentModel.DataAnnotations;

namespace ServiceLibrary.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Barcode { get; set; }
        public required string BaseUnit { get; set; }
        public decimal? Quantity { get; set; }
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; } = true;
        public string? ImagePath { get; set; }
        public required string VatType { get; set; }
        public required virtual Category Category { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }
    }
}
