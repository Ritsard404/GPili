using ServiceLibrary.Utils;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ServiceLibrary.Models
{
    public class Item
    {
        [Key]
        public long Id { get; set; }
        public required decimal Qty { get; set; }
        public required decimal Price { get; set; }
        public required decimal SubTotal { get; set; }
        public required string Status { get; set; }

        public bool IsTrainingMode { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public required virtual Product Product { get; set; }
        public required virtual Invoice Invoice { get; set; }

        [NotMapped]
        public string QtyDisplay => Qty % 1 == 0
            ? ((int)Qty).ToString()
            : Qty.ToString("F2");

        [NotMapped]
        public string DisplayNameWithPrice => $"{Product?.Name} @{Price.PesoFormat():N2}";
        
        [NotMapped]
        public string DisplaySubtotalVat => Product?.VatType == VatType.Vatable ? $"{SubTotal.PesoFormat()}V" :
            Product?.VatType == VatType.Exempt ? $"{SubTotal.PesoFormat()}E" :
            $"{SubTotal.PesoFormat()}Z";

    }
}
