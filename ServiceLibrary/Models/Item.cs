using System.ComponentModel.DataAnnotations;

namespace ServiceLibrary.Models
{
    public class Item
    {
        [Key]
        public long Id { get; set; }
        public required decimal Qty { get; set; }
        public decimal? Price { get; set; }
        public decimal? SubTotal { get; set; }
        public required string Status { get; set; }

        public bool IsVoid { get; set; } = false;
        public bool IsTrainingMode { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public required virtual Product Product { get; set; }
        public required virtual Invoice Invoice { get; set; }
    }
}
