using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceLibrary.Models
{
    public class SaleType
    {
        [Key]
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Account { get; set; }
        public required string Type { get; set; }
        public bool IsActive { get; set; } = true;

        [NotMapped]
        public string Status
        {
            get => IsActive ? "Active" : "Inactive";
            set => IsActive = string.Equals(value, "Active", StringComparison.OrdinalIgnoreCase);
        }
    }
}
