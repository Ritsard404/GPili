namespace ServiceLibrary.Services.DTO.Report
{
    public class GetInvoiceDocumentDTO
    {
        public long Id { get; set; }
        public required string Type { get; set; }
        public required string TypeDisplay { get; set; }
        public required string Status { get; set; }
        public required string CreatedAt { get; set; }
    }
}
