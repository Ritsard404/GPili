using System.ComponentModel.DataAnnotations;

namespace ServiceLibrary.Models
{
    public class EPayment
    {
        [Key]
        public long Id { get; set; }
        public required string Reference { get; set; }
        public required decimal Amount { get; set; }
        public required Invoice Invoice { get; set; }
        public required SaleType SaleType { get; set; }
    }
}
