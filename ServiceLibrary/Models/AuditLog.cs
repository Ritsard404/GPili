using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceLibrary.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Cashier))]
        public string? CashierEmail { get; set; }
        public User? Cashier { get; set; }

        [ForeignKey(nameof(Manager))]
        public string? ManagerEmail { get; set; }
        public User? Manager { get; set; }
        public required string Action { get; set; }
        public required string Changes { get; set; }
        public decimal? Amount { get; set; }
        public bool isTrainMode { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
