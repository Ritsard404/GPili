using System.Text.Json.Serialization;

namespace ServiceLibrary.Services.DTO.Inventory
{
    public class LoadProductsDTO
    {
        [JsonPropertyName("AllUsers")]
        public List<ProductsDTO> Products { get; set; } = new();
    }
    public class ProductsDTO
    {
        [JsonPropertyName("itemid")]
        public string ItemId { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("baseunit")]
        public string BaseUnit { get; set; } = string.Empty;

        [JsonPropertyName("cost")]
        public string Cost { get; set; } = "0";

        [JsonPropertyName("price")]
        public string Price { get; set; } = "0";

        [JsonPropertyName("itemgroup")]
        public string ItemGroup { get; set; } = string.Empty;

        [JsonPropertyName("imagelocation")]
        public string? ImageLocation { get; set; }

        [JsonPropertyName("barcode")]
        public string Barcode { get; set; } = string.Empty;

        [JsonPropertyName("item_type")]
        public string ItemType { get; set; } = string.Empty;

        [JsonPropertyName("vat_type")]
        public string VatType { get; set; } = string.Empty;
    }
}
