namespace ServiceLibrary.Services.DTO.Report
{
    public class InvoiceDTO
    {
        public required BusinesDetails BusinesDetails { get; set; }

        // Invoice Details
        public required string InvoiceNum { get; set; }
        public required string InvoiceDate { get; set; }
        public required string CashierName { get; set; }

        // Items
        public List<ItemInfo> Items { get; set; } = new List<ItemInfo>();

        // Totals
        public required string TotalAmount { get; set; }
        public required string DiscountAmount { get; set; }
        public required string SubTotal { get; set; }
        public required string DueAmount { get; set; }
        public List<OtherPayment> OtherPayments { get; set; } = new List<OtherPayment>();
        public required string CashTenderAmount { get; set; }
        public required string TotalTenderAmount { get; set; }
        public required string ChangeAmount { get; set; }
        public required string VatExemptSales { get; set; }
        public required string VatSales { get; set; }
        public required string VatAmount { get; set; }
        public required string VatZero { get; set; }

        public string? ElligiblePersonDiscount { get; set; }
    }

    public class BusinesDetails
    {
        public required string PosSerialNumber { get; set; } // serial number of the POS machine.
        public required string MinNumber { get; set; } // MIN (Machine Identifier Number) of the POS machine.
        public required string AccreditationNumber { get; set; } // accreditation number of the POS machine.
        public required string PtuNumber { get; set; } // PTU (Point of Transaction Unit) number of the POS machine.
        public required string DateIssued { get; set; } // date when the machine was issued.
        public required string ValidUntil { get; set; } // date until the machine is valid.

        // Business details
        public required string PosName { get; set; } // registered name of the business.
        public required string RegisteredName { get; set; } // registered name of the business.
        public required string OperatedBy { get; set; }
        public required string Address { get; set; } // address of the business.
        public required string VatTinNumber { get; set; } // VAT (Value Added Tax) TIN (Tax Identification Number) of the business.

        public required string CostCenter { get; set; }
        public required string BranchCenter { get; set; }


        public bool IsTrainMode { get; set; } = false;
    }

    public class ItemInfo
    {
        public required string Qty { get; set; } 
        public required string Description { get; set; } 
        public required string Amount { get; set; }
    }

    public class OtherPayment
    {
        public required string SaleTypeName { get; set; } 
        public required string Reference { get; set; } 
        public required string Amount { get; set; } 
    }
}
