namespace ServiceLibrary.Utils
{
    public static class InvoiceStatusType
    {
        public const string Cancelled = "Cancelled";
        public const string Returned = "Returned";
        public const string Void = "Void";
        public const string Pending = "Pending";
        public const string Paid = "Paid";
    }

    public static class InvoiceDocumentType
    {
        public const string Invoice = "Invoice";
        public const string ZReport = "ZReport";
        public const string XReport = "XReport";
    }
}
