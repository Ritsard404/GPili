namespace ServiceLibrary.Services.DTO.Report
{
    public class GetInvoiceDocumentDTO
    {
        public long Id { get; set; }
        public required string Type { get; set; }
        public required string CreatedAt { get; set; }
    }
}
