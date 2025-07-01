namespace ServiceLibrary.Services.DTO.Payment
{
    public class EPaymentDTO
    {
        public required string Reference { get; set; }
        public decimal Amount { get; set; }
        public int SaleTypeId { get; set; }
        public required string SaleTypeName { get; set; }
    }
}
