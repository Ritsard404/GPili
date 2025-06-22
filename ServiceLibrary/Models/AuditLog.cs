using System.ComponentModel.DataAnnotations;

namespace ServiceLibrary.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }
        public User? Cashier { get; set; }
        public User? Manager { get; set; }
        public required string Action { get; set; }
        public required string Changes { get; set; }
        public decimal? Amount { get; set; }
        public bool isTrainMode { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
