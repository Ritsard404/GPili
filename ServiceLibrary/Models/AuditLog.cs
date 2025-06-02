using System.ComponentModel.DataAnnotations;

namespace ServiceLibrary.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }
        public User? Cashier { get; set; }
        public User? Manager { get; set; }
        public required string EntityType { get; set; }
        public required string Action { get; set; }
        public required DateTime CreatedAt { get; set; } = DateTime.Now;
        public required string Changes { get; set; }
    }
}
