using System.ComponentModel.DataAnnotations;

namespace ServiceLibrary.Models
{
    public class PosTerminalInfo
    {
        [Key]
        public required string PosSerialNumber { get; set; } // serial number of the POS machine.
        public required string MinNumber { get; set; } // MIN (Machine Identifier Number) of the POS machine.
        public required string AccreditationNumber { get; set; } // accreditation number of the POS machine.
        public required string PtuNumber { get; set; } // PTU (Point of Transaction Unit) number of the POS machine.
        public required DateTime DateIssued { get; set; } // date when the machine was issued.
        public required DateTime ValidUntil { get; set; } // date until the machine is valid.

        // Business details
        public required string PosName { get; set; } // registered name of the business.
        public required string RegisteredName { get; set; } // registered name of the business.
        public required string OperatedBy { get; set; }
        public required string Address { get; set; } // address of the business.
        public required string VatTinNumber { get; set; } // VAT (Value Added Tax) TIN (Tax Identification Number) of the business.
        public required int Vat { get; set; } // VAT (Value Added Tax)
        public required decimal DiscountMax { get; set; } 

        // API Flags
        public required string CostCenter { get; set; }
        public required string BranchCenter { get; set; }
        public required string UseCenter { get; set; }
        public required string DbName { get; set; }

        public required string PrinterName { get; set; }

        public int ResetCounterNo { get; set; } = 0;
        public int ResetCounterTrainNo { get; set; } = 0;
        public int ZCounterNo { get; set; } = 0;
        public int ZCounterTrainNo { get; set; } = 0;

        public bool IsTrainMode { get; set; } = false;

        // Is Retail Type
        public bool IsRetailType { get; set; } = true;
    }
}
