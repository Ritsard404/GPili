using ServiceLibrary.Utils;
using System.Globalization;
using System.Text.Json.Serialization;

namespace ServiceLibrary.Services.DTO.Report
{
    public class XInvoiceDTO
    {
        public required string BusinessName { get; set; }
        public required string OperatorName { get; set; }
        public required string AddressLine { get; set; }
        public required string VatRegTin { get; set; }
        public required string Min { get; set; }
        public required string SerialNumber { get; set; }

        public required string ReportDate { get; set; }
        public required string ReportTime { get; set; }
        public required string StartDateTime { get; set; }
        public required string EndDateTime { get; set; }

        public required string Cashier { get; set; }
        public required string BeginningOrNumber { get; set; }
        public required string EndingOrNumber { get; set; }
        public required string TransactCount { get; set; }

        public required string OpeningFund { get; set; }

        public required Payments Payments { get; set; }
        public required string VoidAmount { get; set; }
        public required string VoidCount { get; set; }
        public required string Refund { get; set; }
        public required string RefundCount { get; set; }
        public required string Withdrawal { get; set; }

        public required TransactionSummary TransactionSummary { get; set; }

        public required string ShortOver { get; set; }
    }

    public class PaymentDetail
    {
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string AmountString => Amount.PesoFormat();
    }

    public class Payments
    {
        public decimal Cash { get; set; }
        public string CashString => Cash.PesoFormat();

        // Renamed and typed for clarity
        public List<PaymentDetail> OtherPayments { get; set; } = new List<PaymentDetail>();

        public string Total => (Cash + OtherPayments.Sum(p => p.Amount)).PesoFormat();
    }

    public class TransactionSummary
    {
        public required string CashInDrawer { get; set; }

        // Match payments in summary (e.g., cheque, credit card, etc.)
        public List<PaymentDetail> OtherPayments { get; set; } = new List<PaymentDetail>();
    }

}
