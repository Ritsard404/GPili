﻿using ServiceLibrary.Models;
using ServiceLibrary.Services.DTO.Order;
using ServiceLibrary.Services.DTO.Payment;
using ServiceLibrary.Utils;

namespace GPili.Presentation.Features.Cashiering
{
    public partial class InitialItem : ObservableObject
    {

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsInitialDisplay))]
        private decimal _initialQty = 1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsInitialDisplay))]
        private string _qtyBuffer = "";

        public bool IsInitialDisplay => InitialQty > 0;
    
    }
    public partial class PendingItems : ObservableObject
    {
        public Item Item { get; }

        [ObservableProperty]
        private decimal _qty;

        [ObservableProperty]
        private decimal _price;

        [ObservableProperty]
        private decimal _subTotal;
        public PendingItems(Item item)
        {
            Item = item;
            Qty = item.Qty;
            Price = item.Price;
            SubTotal = item.SubTotal;

        }
        partial void OnQtyChanged(decimal value) => Item.Qty = value;
        partial void OnPriceChanged(decimal value) => Item.Price = value;
        partial void OnSubTotalChanged(decimal value) => Item.SubTotal = value;

        public string QtyDisplay => Item.QtyDisplay;
        public string DisplayNameWithPrice => Item.DisplayNameWithPrice;
    }

    public partial class ItemTotals : ObservableObject
    {
        // Tender
        [ObservableProperty]
        private decimal _cashTenderAmount = 0m;

        [ObservableProperty]
        private string _payBuffer = "";

        // Other payment
        [ObservableProperty]
        private ObservableCollection<EPaymentDTO> _otherPayments = new();
        public bool HasOtherPayments => OtherPayments != null && OtherPayments.Count > 0;
        // Items to be paid
        [ObservableProperty]
        private ObservableCollection<Item> _itemsToPaid = new();

        // Discount
        [ObservableProperty]
        private DiscountDTO? _discount = new();

        // VAT and Discount config
        public decimal VatRate => POSInfo.Terminal.Vat / 100m;
        public decimal DiscountMax => POSInfo.Terminal.DiscountMax;

        // Computed VAT breakdown
        public decimal VatableTotal => ItemsToPaid
            .Where(v => v.Product.VatType == VatType.Vatable)
            .Sum(i => i.SubTotal);

        public decimal VatSales => VatableTotal / (1 + VatRate);
        public decimal VatAmount => VatableTotal - VatSales;
        public decimal VatExemptSales => ItemsToPaid.Where(v => v.Product.VatType == VatType.Exempt)
            .Sum(i => i.SubTotal);
        public decimal VatZero => ItemsToPaid.Where(v => v.Product.VatType == VatType.Zero)
            .Sum(i => i.SubTotal);

        // Totals
        public decimal GrossTotal => ItemsToPaid.Sum(i => i.SubTotal);
        public decimal DiscountAmount
        {
            get
            {

                if (Discount?.DiscountAmount.GetValueOrDefault() > 0)
                {
                    return Discount.DiscountAmount.GetValueOrDefault() > DiscountMax
                        ? DiscountMax
                        : Discount.DiscountAmount.GetValueOrDefault();
                }

                var pct = Discount?.DiscountPercent ?? 0m;
                if (pct <= 0) return 0m;

                var raw = GrossTotal * (pct / 100m); // Use GrossTotal here
                return raw > DiscountMax
                    ? DiscountMax
                    : raw;
            }
        }
        public decimal TotalAmount => GrossTotal - DiscountAmount;
        public decimal AmountDue => TotalAmount;
        public decimal SubTotal => AmountDue - VatAmount;

        // Tender & changepublic
        public decimal TenderAmount =>
            CashTenderAmount + Math.Min(OtherPayments?.Sum(a => a.Amount) ?? 0, Math.Max(0, TotalAmount - CashTenderAmount));

        //public decimal TenderAmount => CashTenderAmount + (OtherPayments?.Sum(a => a.Amount) ?? 0);
        public decimal ChangeAmount => TenderAmount - TotalAmount;

        public bool IsExactPayEnable => CashTenderAmount == 0 && !HasOtherPayments; 
        public void SetExactCashAmount() => CashTenderAmount = AmountDue;

        // Helper to raise all calculated property changes
        private void NotifyAllTotalsChanged()
        {
            OnPropertyChanged(nameof(GrossTotal));
            OnPropertyChanged(nameof(TenderAmount));
            OnPropertyChanged(nameof(DiscountAmount));
            OnPropertyChanged(nameof(ChangeAmount));
            OnPropertyChanged(nameof(AmountDue));
            OnPropertyChanged(nameof(SubTotal));
            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(VatableTotal));
            OnPropertyChanged(nameof(VatSales));
            OnPropertyChanged(nameof(VatAmount));
            OnPropertyChanged(nameof(VatExemptSales));
            OnPropertyChanged(nameof(VatZero));
            OnPropertyChanged(nameof(HasOtherPayments));
            OnPropertyChanged(nameof(IsExactPayEnable));

        }

        // Partial methods generated by [ObservableProperty]
        partial void OnCashTenderAmountChanged(decimal oldValue, decimal newValue) => NotifyAllTotalsChanged();
        partial void OnOtherPaymentsChanged(ObservableCollection<EPaymentDTO>? oldValue, ObservableCollection<EPaymentDTO> newValue) => NotifyAllTotalsChanged();
        partial void OnItemsToPaidChanged(ObservableCollection<Item>? oldValue, ObservableCollection<Item> newValue) => NotifyAllTotalsChanged();
        partial void OnDiscountChanged(DiscountDTO? oldValue, DiscountDTO? newValue) => NotifyAllTotalsChanged();

    }
}
