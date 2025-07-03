using System.ComponentModel.DataAnnotations;

namespace ServiceLibrary.Models
{
    public class Product
    {
        [Key]
        public long Id { get; set; }
        public required string ProdId { get; set; }
        public required string Name { get; set; }
        public required string Barcode { get; set; }
        public required string BaseUnit { get; set; }
        public decimal? Quantity { get; set; }
        public decimal Cost { get; set; }
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; } = true;
        public required string ItemType { get; set; }
        public required string VatType { get; set; }
        public required virtual Category Category { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; }
    }
}
