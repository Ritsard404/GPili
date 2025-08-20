using ServiceLibrary.Models;
using ServiceLibrary.Services.Interfaces;
using ServiceLibrary.Services.DTO.Payment;

namespace GPili.Mobile.Presentation.Popups
{
    public partial class EPaymentViewModel(IEPayment _ePayment) : ObservableValidator
    {

        public EPaymentView Popup;

        [ObservableProperty]
        private ObservableCollection<EPaymentEntryViewModel> _paymentEntries = new();

        public double PopupWidth => Shell.Current.CurrentPage.Width * 0.95;
        public double PopupHeight => Shell.Current.CurrentPage.Height * 0.8;

        public async Task LoadSaleTypes()
        {
            PaymentEntries.Clear();
            var saleTypes = await _ePayment.SaleTypes();

            if (saleTypes == null || !saleTypes.Any())
            {
                await Snackbar.Make(
                    message: "No E-Payments yet!",
                    duration: TimeSpan.FromSeconds(1)).Show();
                Popup.Close();
                return;
            }

            foreach (var saleType in saleTypes)
            {
                PaymentEntries.Add(new EPaymentEntryViewModel(saleType));
            }
        }

        private ObservableCollection<EPaymentDTO> ToObservableCollection()
        {
            var result = new ObservableCollection<EPaymentDTO>();
            foreach (var entry in PaymentEntries)
            {
                if (!string.IsNullOrWhiteSpace(entry.Reference) && entry.Amount.HasValue && entry.Amount.Value > 0)
                {
                    result.Add(new EPaymentDTO
                    {
                        Reference = entry.Reference!.ToUpper(),
                        Amount = entry.Amount.Value,
                        SaleTypeId = entry.SaleType.Id,
                        SaleTypeName = entry.SaleType.Name
                    });
                }
            }
            return result;
        }

        [RelayCommand]
        private async Task Submit()
        {
            // Validate only filled entries
            bool hasError = false;
            foreach (var entry in PaymentEntries)
            {
                if (entry.IsFilled)
                {
                    if (!entry.IsValid)
                        hasError = true;
                }
            }


            if (hasError)
            {
                await Snackbar.Make(
                    message: "Please ensure all filled E-Payment entries have both a valid reference and an amount greater than 0.",
                    duration: TimeSpan.FromSeconds(1)).Show();
                return;
            }

            var result = ToObservableCollection();
            Popup?.CloseWithResult(result);
        }
    }

    public partial class EPaymentEntryViewModel : ObservableValidator
    {
        public SaleType SaleType { get; }

        [ObservableProperty]
        private string? _reference;

        [ObservableProperty]
        private decimal? _amount;

        public string Name => SaleType.Name;

        public EPaymentEntryViewModel(SaleType saleType)
        {
            SaleType = saleType;
        }

        partial void OnReferenceChanged(string? value)
        {
            ValidateProperty(value, nameof(Reference));
        }

        partial void OnAmountChanged(decimal? value)
        {
            if (value.HasValue)
                _amount = Math.Round(value.Value, 2); // Use backing field to avoid recursion
            ValidateProperty(value, nameof(Amount));
        }

        public bool IsFilled => !string.IsNullOrWhiteSpace(Reference) || (Amount.HasValue && Amount.Value > 0);

        public bool IsValid
        {
            get
            {
                // Validate only if entry is filled
                if (!IsFilled)
                    return true;

                bool hasReference = !string.IsNullOrWhiteSpace(Reference);
                bool hasAmount = Amount.HasValue && Amount.Value > 0;

                if (!hasReference || !hasAmount || Amount <= 0)
                    return false;

                return !HasErrors;
            }
        }
    }
}
