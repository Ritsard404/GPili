
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceLibrary.Models
{
    public class Journal
    {
        [Key]
        public long UniqueId { get; set; }  // NOT NULL, AUTO_INCREMENT
        public string Entry_Type { get; set; } = "INVOICE";
        public required string Entry_No { get; set; }
        public required string Entry_Line_No { get; set; }
        public required string Entry_Date { get; set; }
        public required string CostCenter { get; set; }
        public required string ItemId { get; set; }
        public required string Unit { get; set; }
        public required string Qty { get; set; }
        public required string Cost { get; set; }
        public required string Price { get; set; }
        public required string TotalPrice { get; set; }
        public required string Debit { get; set; }
        public required string Credit { get; set; }
        public required string AccountBalance { get; set; }
        public required string Prev_Reading { get; set; }
        public required string Curr_Reading { get; set; }
        public required string Memo { get; set; }
        public required string AccountName { get; set; }
        public required string Reference { get; set; }
        public required string Entry_Name { get; set; }
        public required string Cashier { get; set; }
        public required string Count_Type { get; set; }
        public required string Deposited { get; set; }
        public required string Deposit_Date { get; set; }
        public required string Deposit_Reference { get; set; }
        public required string Deposit_By { get; set; }
        public required string Deposit_Time { get; set; }
        public required string CustomerName { get; set; }
        public required string SubTotal { get; set; }
        public required string TotalTax { get; set; }
        public required string GrossTotal { get; set; }
        public required string Discount_Type { get; set; }
        public required string Discount_Amount { get; set; }
        public required string NetPayable { get; set; }
        public required string Status { get; set; }
        public required string User_Email { get; set; }
        public required string QtyPerBaseUnit { get; set; }
        public required string QtyBalanceInBaseUnit { get; set; }
        public required bool IsPushed { get; set; } = false;
    }

}
