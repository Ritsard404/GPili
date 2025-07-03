using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ServiceLibrary.Models
{
    public class User
    {
        [Key]
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public required string FName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public required string LName { get; set; }

        [Required(ErrorMessage = "User role is required")]
        public required string Role { get; set; }

        [JsonIgnore]
        public bool IsActive { get; set; } = true;
        [JsonIgnore]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        [JsonIgnore]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [JsonIgnore]
        [NotMapped]
        public string FullName => $"{FName} {LName}";

        [NotMapped]
        public string Status
        {
            get => IsActive ? "Active" : "Inactive";
            set => IsActive = string.Equals(value, "Active", StringComparison.OrdinalIgnoreCase);
        }
    }
}
