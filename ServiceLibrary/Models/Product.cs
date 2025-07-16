using ServiceLibrary.Utils;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceLibrary.Models
{
    public class Product
    {
        [Key]
        public long Id { get; set; }
        public string ProdId { get; set; } = string.Empty;
        public required string Name { get; set; }
        public string? ImagePath { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string BaseUnit { get; set; } = string.Empty;
        public decimal? Quantity { get; set; }
        public decimal Cost { get; set; } = 0;
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; } = true;
        public string ItemType { get; set; } = string.Empty;
        public required string VatType { get; set; } = "Vatable";
        public required virtual Category Category { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public bool HasImage => !string.IsNullOrEmpty(ImagePath) && File.Exists(ImagePath);

        [NotMapped]
        public string ImageSourcePath
        {
            get
            {
                if (string.IsNullOrEmpty(ImagePath))
                    return null;
                if (ImagePath.StartsWith("file://") || ImagePath.StartsWith("http"))
                    return ImagePath;
                if (System.IO.Path.IsPathRooted(ImagePath))
                    return new Uri(ImagePath).AbsoluteUri;
                return ImagePath;
            }
        }
      
    }
}
