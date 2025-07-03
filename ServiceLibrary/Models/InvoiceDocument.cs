using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace ServiceLibrary.Models
{
    public class InvoiceDocument
    {
        [Key]
        public long Id { get; set; }
        public byte[]? InvoiceBlob { get; set; }
        public required string Type { get; set; }
        public int ReprintCount { get; set; } = 1;
        public Invoice? Invoice { get; set; }
        public User? Manager { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
