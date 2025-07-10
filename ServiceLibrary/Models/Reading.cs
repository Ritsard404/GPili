using System.ComponentModel.DataAnnotations;

namespace ServiceLibrary.Models
{
    public class Reading
    {
        [Key]
        public long Id { get; set; }
        public string LastInvoice { get; set; }
        public decimal Previous { get; set; }
        public decimal Present { get; set; }
        public decimal Sales { get; set; }
        public bool IsTrainMode { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
